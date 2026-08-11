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

    private string NuGetGetUri => configuration["NuGet:CatalogsUri"]
                                  ?? throw new InvalidOperationException("NuGet Catalog URI не сконфигурирована.");

    private HttpResponseValidationPipeline? _pipeline;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("NuGetPackageCheckerService running at: {Time}", DateTimeOffset.UtcNow);
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IPUContext>();
        _pipeline = scope.ServiceProvider.GetRequiredService<HttpResponseValidationPipeline>();

        await CheckForNewPackagesAsync(context, stoppingToken);
    }


    private async Task CheckForNewPackagesAsync(IPUContext context, CancellationToken stoppingToken)
    {
        var catalogList = await GetFromJson<CatalogListModel>(NuGetGetUri, stoppingToken);

        foreach (var pageBatch in catalogList.Items.Chunk(4000)) // CatalogPage
            await ProcessCatalogPagesAsync(context, pageBatch, stoppingToken); // по 4000 мета-страниц за раз
    }

    private async Task ProcessCatalogPagesAsync(IPUContext context, IEnumerable<CatalogPage> batch, CancellationToken stoppingToken)
    {
        var catalogs = new List<CatalogModel>();

        // Обрабатываем страницы последовательно для каждого батча, чтобы избежать проблем с DbContext
        foreach (var page in batch)
        {
            if (stoppingToken.IsCancellationRequested)
                stoppingToken.ThrowIfCancellationRequested();

            try
            {
                var catalog = await GetFromJson<CatalogModel>(page.NuGetUri, stoppingToken);
                catalogs.Add(catalog);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Ошибка при обработке страницы каталога: {Uri}", page.NuGetUri);
            }
        }

        await ProcessPackageBatchesAsync(context, catalogs, stoppingToken);
    }

    private async Task ProcessPackageBatchesAsync(IPUContext context, IEnumerable<CatalogModel> catalogs, CancellationToken stoppingToken)
    {
        var allUris = catalogs.SelectMany(c => c.Items.Select(p => p.NuGetUri)).ToList();

        foreach (var batch in allUris.Chunk(100)) // Уменьшенный размер батча для надежной работы с EF Core
        {
            if (stoppingToken.IsCancellationRequested)
                stoppingToken.ThrowIfCancellationRequested();

            try
            {
                await ProcessPackageBatchAsync(context, batch, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Ошибка при обработке батча пакетов");
            }

            // Сохраняем после каждого батча
            try
            {
                await context.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Ошибка при сохранении батча в базу данных");
            }
        }
    }

    private async Task ProcessPackageBatchAsync(IPUContext context, IEnumerable<string> batchUris, CancellationToken cancellationToken)
    {
        // Обрабатываем пакеты последовательно внутри батча для избежания проблем с трекингом EF Core
        foreach (var uri in batchUris)
        {
            if (cancellationToken.IsCancellationRequested)
                cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var pkg = await GetPackageDetailAsync(uri, cancellationToken);
                if (pkg != null)
                    await SavePackageDetailAsync(context, pkg, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Ошибка при обработке пакета: {Uri}", uri);
            }
        }
    }

    private async Task SavePackageDetailAsync(
        IPUContext context,
        PackageDetailModel pkg,
        CancellationToken cancellationToken)
    {
        // 1. Find or create the main Package
        var packageEntity = await context.Packages
            .FirstOrDefaultAsync(p => p.NugetId == pkg.PackageId, cancellationToken);

        if (packageEntity == null)
        {
            packageEntity = new Package
            {
                NugetId = pkg.PackageId,
                Description = pkg.Description,
                IsRecommended = false
            };
            context.Packages.Add(packageEntity);
            await context.SaveChangesAsync(cancellationToken);
        }

        // 2. Find or create the specific version
        var versionEntity = await context.PackageVersions
            .FirstOrDefaultAsync(v =>
                v.PackageId == packageEntity.Id &&
                v.Version == pkg.Version, cancellationToken);

        if (versionEntity == null)
        {
            versionEntity = new PackageVersion
            {
                PackageId = packageEntity.Id,
                Version = pkg.Version,
                PublishedAt = pkg.Published,
                IsPrerelease = pkg.IsPrerelease,
                TargetFramework = string.Empty,
                PackageUrl = pkg.PackageId
            };
            context.PackageVersions.Add(versionEntity);
            await context.SaveChangesAsync(cancellationToken);
        }

        // 3. Process dependencies sequentially
        foreach (var group in pkg.DependencyGroups)
        foreach (var dep in group.Dependencies)
        {
            if (cancellationToken.IsCancellationRequested)
                cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var targetPackage = await context.Packages
                    .FirstOrDefaultAsync(p => p.NugetId == dep.DependencyId, cancellationToken);

                if (targetPackage == null)
                {
                    logger.LogDebug("Пропущена зависимость. Пакет {DepId} не найден", dep.DependencyId);
                    continue;
                }

                var alreadyExists = await context.PackageDependencies.AnyAsync(d =>
                    d.SourceVersionId == versionEntity.Id &&
                    d.TargetPackageId == targetPackage.Id &&
                    d.TargetVersionRange == dep.Range &&
                    d.TargetFramework == group.TargetFramework, cancellationToken);

                if (!alreadyExists)
                {
                    var dependency = new PackageDependency
                    {
                        SourceVersionId = versionEntity.Id,
                        TargetPackageId = targetPackage.Id,
                        TargetVersionRange = dep.Range,
                        TargetFramework = group.TargetFramework
                    };
                    context.PackageDependencies.Add(dependency);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Ошибка при обработке зависимости {DepId}", dep.DependencyId);
            }
        }
    }

    private async Task<PackageDetailModel?> GetPackageDetailAsync(string uri, CancellationToken cancellationToken)
    {
        return await GetFromJson<PackageDetailModel>(uri, cancellationToken);
    }

    private async Task<T> GetFromJson<T>(string uri, CancellationToken cancellationToken) where T : class
    {
        if (string.IsNullOrWhiteSpace(uri))
            throw new ArgumentException("Параметр URI не может быть пустым.", nameof(uri));

        if (_pipeline == null)
            throw new InvalidOperationException("Pipeline не инициализирован. Убедитесь, что сервис запущен корректно.");

        using var response = await httpClient.GetAsync(uri, cancellationToken);
        
        // Проверяем успешность ответа перед валидацией
        if (!response.IsSuccessStatusCode)
        {
            logger.LogDebug("HTTP запрос вернул статус {Status} для URI: {Uri}", response.StatusCode, uri);
            throw new HttpRequestException($"Запрос не удался: {response.StatusCode}");
        }

        await _pipeline.ValidateAsync(new HttpValidationContext(response, uri),
            [HttpValidationTag.ExpectBody, HttpValidationTag.Get]);

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var tModel = await JsonSerializer.DeserializeAsync<T>(stream, CachedJsonSerializerOptions, cancellationToken);
        return tModel ?? throw new JsonException($"Не удалось десериализовать объект типа {typeof(T).Name} из {uri}");
    }
}
