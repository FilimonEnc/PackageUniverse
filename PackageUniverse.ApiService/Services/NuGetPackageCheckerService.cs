using System.Collections.Concurrent;
using System.Text.Json;
using Mapster;
using Microsoft.EntityFrameworkCore;
using PackageUniverse.ApiService.Utils;
using PackageUniverse.ApiService.Validators;
using PackageUniverse.Application.Interfaces;
using PackageUniverse.Application.Models;
using PackageUniverse.Application.Models.NuGetModels;
using PackageUniverse.Core.Entities;

namespace PackageUniverse.ApiService.Services;

public class NuGetPackageCheckerService(
    ILogger<NuGetPackageCheckerService> logger,
    IServiceProvider serviceProvider,
    HttpClient httpClient,
    IConfiguration configuration) : BackgroundService
{
    private static readonly JsonSerializerOptions CachedJsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private IPUContext _context = null!;
    private HttpResponseValidationPipeline? _pipeline;

    private string NuGetGetUri => configuration["NuGet:CatalogsUri"]
                                  ?? throw new InvalidOperationException("NuGet Catalog URI не сконфигурирована.");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("NuGetPackageCheckerService running at: {Time}", DateTimeOffset.UtcNow);
        using var scope = serviceProvider.CreateScope();
        _context = scope.ServiceProvider.GetRequiredService<IPUContext>();
        _pipeline = scope.ServiceProvider.GetRequiredService<HttpResponseValidationPipeline>();

        await CheckForNewPackagesAsync(stoppingToken);
    }


    private async Task CheckForNewPackagesAsync(CancellationToken stoppingToken)
    {
        if (_context is null) throw new InvalidOperationException("Context не инициализирован");

        var catalogList = await GetFromJson<CatalogListModel>(NuGetGetUri, stoppingToken);

        foreach (var pageBatch in catalogList.Items.Chunk(4000)) // CatalogPage
            await ProcessCatalogPagesAsync(pageBatch, stoppingToken); // по 4000 мета-страниц за раз
    }

    private async Task ProcessCatalogPagesAsync(IEnumerable<CatalogPage> batch, CancellationToken stoppingToken)
    {
        var catalogs = new ConcurrentBag<CatalogModel>();

        await BatchProcessor.ForEachAsync(batch, 16, async page =>
        {
            var catalog = await GetFromJson<CatalogModel>(page.NuGetUri, stoppingToken);
            catalogs.Add(catalog);
        }, logger);


        await ProcessPackageBatchesAsync(catalogs, stoppingToken);
    }

    private async Task ProcessPackageBatchesAsync(IEnumerable<CatalogModel> catalogs, CancellationToken stoppingToken)
    {
        var allUris = catalogs.SelectMany(c => c.Items.Select(p => p.NuGetUri)).ToList();
        var throttle = new SemaphoreSlim(16);

        foreach (var batch in allUris.Chunk(4000))
        {
            using var batchScope = serviceProvider.CreateScope();
            var batchContext = batchScope.ServiceProvider.GetRequiredService<IPUContext>();

            var deferredDependencySaves = new ConcurrentBag<Func<Task>>();

            var tasks = batch.Select(async uri =>
            {
                await throttle.WaitAsync(stoppingToken);
                try
                {
                    var pkg = await GetFromJson<PackageDetailModel>(uri, stoppingToken);
                    using var taskScope = serviceProvider.CreateScope();
                    var taskContext = taskScope.ServiceProvider.GetRequiredService<IPUContext>();
                    await SavePackageDetailAsync(pkg, taskContext, deferredDependencySaves, batchContext, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Ошибка при обработке пакета: {Uri}", uri);
                }
                finally
                {
                    throttle.Release();
                }
            });

            await Task.WhenAll(tasks);

            // Deferred dependency saves — sequential on batchContext (no concurrency issues)
            foreach (var save in deferredDependencySaves)
            {
                try
                {
                    await save();
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Ошибка при сохранении зависимости");
                }
            }

            await batchContext.SaveChangesAsync(stoppingToken);
        }
    }

    private async Task SavePackageDetailAsync(
        PackageDetailModel pkg,
        IPUContext taskContext,
        ConcurrentBag<Func<Task>> deferredDependencySaves,
        IPUContext batchContext,
        CancellationToken cancellationToken)
    {
        // 1. Find or create the main Package
        var packageEntity = await taskContext.Packages
            .FirstOrDefaultAsync(p => p.NuGetUri == pkg.PackageId, cancellationToken);

        if (packageEntity == null)
        {
            packageEntity = pkg.Adapt<Package>();
            taskContext.Packages.Add(packageEntity);
            await taskContext.SaveChangesAsync(cancellationToken);
        }

        // 2. Find or create the specific version
        var versionEntity = await taskContext.PackageVersions
            .FirstOrDefaultAsync(v =>
                v.PackageId == packageEntity.Id &&
                v.Version == pkg.Version, cancellationToken);

        if (versionEntity == null)
        {
            versionEntity = pkg.Adapt<PackageVersion>();
            versionEntity.PackageId = packageEntity.Id;

            taskContext.PackageVersions.Add(versionEntity);
            await taskContext.SaveChangesAsync(cancellationToken);
        }

        // 3. Defer dependency resolution — uses batchContext (sequential, after all tasks complete)
        var capturedVersionId = versionEntity.Id;

        foreach (var group in pkg.DependencyGroups)
        foreach (var dep in group.Dependencies)
            deferredDependencySaves.Add(async () =>
            {
                var targetPackage = await batchContext.Packages
                    .FirstOrDefaultAsync(p => p.NuGetUri == dep.DependencyId, cancellationToken);

                if (targetPackage == null)
                {
                    logger.LogDebug("Пропущена зависимость. Пакет {DepId} не найден", dep.DependencyId);
                    return;
                }

                var alreadyExists = await batchContext.PackageDependencies.AnyAsync(d =>
                    d.SourceVersionId == capturedVersionId &&
                    d.TargetPackageId == targetPackage.Id &&
                    d.TargetVersionRange == dep.Range &&
                    d.TargetFramework == group.TargetFramework, cancellationToken);

                if (!alreadyExists)
                {
                    var dependency = dep.Adapt<PackageDependency>();
                    dependency.SourceVersionId = capturedVersionId;
                    dependency.TargetPackageId = targetPackage.Id;
                    dependency.TargetFramework = group.TargetFramework;

                    batchContext.PackageDependencies.Add(dependency);
                }
            });
    }

    private async Task<T> GetFromJson<T>(string uri, CancellationToken cancellationToken) where T : class
    {
        if (string.IsNullOrWhiteSpace(uri))
            throw new ArgumentException("Параметр URI не может быть пустым.", nameof(uri));
        if (_pipeline is null)
            throw new InvalidOperationException("Validation pipeline не инициализирован.");

        using var response = await httpClient.GetAsync(uri, cancellationToken);
        await _pipeline.ValidateAsync(new HttpValidationContext(response, uri),
            [HttpValidationTag.ExpectBody, HttpValidationTag.Get]);

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var tModel = await JsonSerializer.DeserializeAsync<T>(stream, CachedJsonSerializerOptions, cancellationToken);
        return tModel ?? throw new JsonException($"Не удалось десериализовать объект типа {typeof(T).Name} из {uri}");
    }
}
