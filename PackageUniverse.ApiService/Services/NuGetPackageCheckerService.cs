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

    private readonly List<Func<Task>> _deferredDependencySaves = new();

    private IPUContext _context = null!;
    private HttpResponseValidationPipeline? _pipeline;

    private string NuGetGetUri => configuration["NuGet:CatalogsUri"]
                                  ?? throw new InvalidOperationException("NuGet Catalog URI не сконфигурирована.");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("NuGetPackageCheckerService running at: {Time}", DateTimeOffset.Now);
        using var scope = serviceProvider.CreateScope();
        _context = scope.ServiceProvider.GetRequiredService<IPUContext>();
        _pipeline = scope.ServiceProvider.GetRequiredService<HttpResponseValidationPipeline>();

        await CheckForNewPackagesAsync();
    }


    private async Task CheckForNewPackagesAsync()
    {
        if (_context is null) throw new InvalidOperationException("Context не инициализирован");

        var catalogList = await GetFromJson<CatalogListModel>(NuGetGetUri);

        foreach (var pageBatch in catalogList.Items.Chunk(4000)) // CatalogPage
            await ProcessCatalogPagesAsync(pageBatch); // по 4000 мета-страниц за раз
    }

    private async Task ProcessCatalogPagesAsync(IEnumerable<CatalogPage> batch)
    {
        var catalogs = new ConcurrentBag<CatalogModel>();

        await BatchProcessor.ForEachAsync(batch, 16, async page =>
        {
            var catalog = await GetFromJson<CatalogModel>(page.NuGetUri);
            catalogs.Add(catalog);
        }, logger);


        await ProcessPackageBatchesAsync(catalogs);
    }

    private async Task ProcessPackageBatchesAsync(IEnumerable<CatalogModel> catalogs)
    {
        var allUris = catalogs.SelectMany(c => c.Items.Select(p => p.NuGetUri)).ToList();
        var throttle = new SemaphoreSlim(16);

        foreach (var batch in allUris.Chunk(4000))
        {
            var tasks = batch.Select(async uri =>
            {
                await throttle.WaitAsync();
                try
                {
                    var pkg = await GetFromJson<PackageDetailModel>(uri); // получаем данные о пакете
                    //сохраняем пакет в БД
                    await SavePackageDetailAsync(pkg);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Ошибка при обработке пакета: {PackageId}", uri);
                }
                finally
                {
                    throttle.Release();
                }
            });

            await Task.WhenAll(tasks); // ждем завершения всех запросов в батче

            foreach (var saveDependency in
                     _deferredDependencySaves) await saveDependency(); // исполняем отложенные добавления зависимостей

            await _context.SaveChangesAsync(); // итоговый flush
        }
    }

    private async Task SavePackageDetailAsync(PackageDetailModel pkg)
    {
        // 1. Найти или создать основной Package
        var packageEntity = await _context.Packages
            .FirstOrDefaultAsync(p => p.NugetId == pkg.PackageId);

        if (packageEntity == null)
        {
            packageEntity = pkg.Adapt<Package>();
            _context.Packages.Add(packageEntity);
            await _context.SaveChangesAsync(); // получаем ID
        }

        // 2. Найти или создать конкретную версию
        var versionEntity = await _context.PackageVersions
            .FirstOrDefaultAsync(v =>
                v.PackageId == packageEntity.Id &&
                v.Version == pkg.Version);

        if (versionEntity == null)
        {
            versionEntity = pkg.Adapt<PackageVersion>();
            versionEntity.PackageId = packageEntity.Id; // Здесь используем правильный тип (int)


            _context.PackageVersions.Add(versionEntity);
            await _context.SaveChangesAsync(); // получаем ID для зависимостей
        }

        // 3. Отложенная обработка зависимостей
        foreach (var group in pkg.DependencyGroups)
        foreach (var dep in group.Dependencies)
            _deferredDependencySaves.Add(async () =>
            {
                var targetPackage = await _context.Packages
                    .FirstOrDefaultAsync(p => p.NugetId == dep.DependencyId);

                if (targetPackage == null)
                {
                    logger.LogWarning("Пропущена зависимость. Пакет {DepId} не найден", dep.DependencyId);
                    return;
                }

                var alreadyExists = await _context.PackageDependencies.AnyAsync(d =>
                    d.SourceVersionId == versionEntity.Id &&
                    d.TargetPackageId == targetPackage.Id &&
                    d.TargetVersionRange == dep.Range &&
                    d.TargetFramework == group.TargetFramework);

                if (!alreadyExists)
                {
                    var dependency = dep.Adapt<PackageDependency>();
                    dependency.SourceVersionId = versionEntity.Id;
                    dependency.TargetPackageId = targetPackage.Id;
                    dependency.TargetFramework = group.TargetFramework;

                    _context.PackageDependencies.Add(dependency);
                }
            });
    }

    private async Task<T> GetFromJson<T>(string uri) where T : Model
    {
        if (string.IsNullOrWhiteSpace(uri))
            throw new ArgumentException("Параметр URI не может быть пустым.", nameof(uri));
        if (_pipeline is null)
            throw new InvalidOperationException("Validation pipeline не инициализирован.");

        var response = await httpClient.GetAsync(uri);
        await _pipeline.ValidateAsync(new HttpValidationContext(response, uri),
            [HttpValidationTag.ExpectBody, HttpValidationTag.Get]);

        await using var stream = await response.Content.ReadAsStreamAsync();
        var tModel = await JsonSerializer.DeserializeAsync<T>(stream, CachedJsonSerializerOptions);
        return tModel ?? throw new JsonException($"Не удалось десериализовать объект типа {typeof(T).Name} из {uri}");
    }
}