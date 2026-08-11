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
    // Ограничитель параллелизма для HTTP запросов, чтобы не упереться в лимты или память
    private readonly SemaphoreSlim _httpSemaphore = new(50); 

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

        foreach (var pageBatch in catalogList.Items.Chunk(4000)) 
            await ProcessCatalogPagesAsync(context, pageBatch, stoppingToken);
    }

    private async Task ProcessCatalogPagesAsync(IPUContext context, IEnumerable<CatalogPage> batch, CancellationToken stoppingToken)
    {
        var catalogs = new List<CatalogModel>();

        // Параллельная загрузка страниц каталога с ограничением конкуренции
        var tasks = batch.Select(async page =>
        {
            try
            {
                return await GetFromJson<CatalogModel>(page.NuGetUri, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Ошибка при обработке страницы каталога: {Uri}", page.NuGetUri);
                return null;
            }
        });

        var results = await Task.WhenAll(tasks);
        catalogs.AddRange(results.Where(c => c != null)!);

        await ProcessPackageBatchesAsync(context, catalogs, stoppingToken);
    }

    private async Task ProcessPackageBatchesAsync(IPUContext context, IEnumerable<CatalogModel> catalogs, CancellationToken stoppingToken)
    {
        var allUris = catalogs.SelectMany(c => c.Items.Select(p => p.NuGetUri)).ToList();
        logger.LogInformation("Найдено {Count} пакетов для обработки", allUris.Count);

        // Разбиваем на батчи для обработки
        foreach (var batch in allUris.Chunk(500)) 
        {
            if (stoppingToken.IsCancellationRequested)
                stoppingToken.ThrowIfCancellationRequested();

            try
            {
                await ProcessPackageBatchAsync(context, batch, stoppingToken);
                logger.LogDebug("Батч из {Count} пакетов обработан", batch.Length);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Ошибка при обработке батча пакетов");
            }
        }
    }

    private async Task ProcessPackageBatchAsync(IPUContext context, IEnumerable<string> batchUris, CancellationToken cancellationToken)
    {
        // Сначала параллельно загружаем все данные через HTTP (это безопасно)
        var packageDetails = new List<PackageDetailModel?>();
        var tasks = batchUris.Select(async uri =>
        {
            try
            {
                await _httpSemaphore.WaitAsync(cancellationToken);
                var pkg = await GetPackageDetailAsync(uri, cancellationToken);
                return pkg;
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Ошибка при загрузке пакета: {Uri}", uri);
                return null;
            }
            finally
            {
                _httpSemaphore.Release();
            }
        });

        var results = await Task.WhenAll(tasks);
        packageDetails.AddRange(results.Where(p => p != null));

        // Затем последовательно обрабатываем и сохраняем в БД
        // SavePackageDetailAsync теперь сам делает SaveChanges для каждой сущности,
        // поэтому общий SaveChanges здесь не нужен.
        foreach (var pkg in packageDetails)
        {
            if (pkg != null)
            {
                try
                {
                    await SavePackageDetailAsync(context, pkg, cancellationToken);
                }
                catch (Exception ex)
                {
                    logger.LogDebug(ex, "Ошибка при сохранении пакета {PackageId}", pkg.PackageId);
                }
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
            
            // ВАЖНО: Сохраняем пакет сразу, чтобы получить сгенерированный БД Id
            // Это необходимо для создания связей с версиями и зависимостями
            await context.SaveChangesAsync(cancellationToken);
        }

        // 2. Find or create the specific version
        int packageId = packageEntity.Id;
        
        var versionEntity = await context.PackageVersions
            .FirstOrDefaultAsync(v =>
                v.PackageId == packageId &&
                v.Version == pkg.Version, cancellationToken);

        if (versionEntity == null)
        {
            versionEntity = new PackageVersion
            {
                PackageId = packageId,
                Version = pkg.Version,
                PublishedAt = pkg.Published,
                IsPrerelease = pkg.IsPrerelease,
                TargetFramework = string.Empty,
                PackageUrl = pkg.PackageId
            };
            context.PackageVersions.Add(versionEntity);
            
            // Сохраняем версию сразу, чтобы получить Id для зависимостей
            await context.SaveChangesAsync(cancellationToken);
        }
        else
        {
            // Версия уже есть, зависимости скорее всего тоже, можно пропустить для экономии времени
            return; 
        }

        // 3. Process dependencies
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
                    // Зависимости может еще не быть в базе, создаем заглушку
                    targetPackage = new Package
                    {
                        NugetId = dep.DependencyId,
                        Description = null,
                        IsRecommended = false
                    };
                    context.Packages.Add(targetPackage);
                    await context.SaveChangesAsync(cancellationToken);
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
                    // Зависимости сохраняем в конце батча или сразу - не критично, но лучше сразу для надежности
                    await context.SaveChangesAsync(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Ошибка при обработке зависимости {DepId}", dep.DependencyId);
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
        
        // Исправление NullReferenceException: строгая проверка статуса перед использованием контента
        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                logger.LogDebug("Пакет не найден (404): {Uri}", uri);
                return null;
            }
            
            logger.LogDebug("HTTP запрос вернул статус {Status} для URI: {Uri}", response.StatusCode, uri);
            throw new HttpRequestException($"Запрос не удался: {response.StatusCode}");
        }

        // Валидация через пайплайн
        try 
        {
            await _pipeline.ValidateAsync(new HttpValidationContext(response, uri),
                [HttpValidationTag.ExpectBody, HttpValidationTag.Get]);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Валидация ответа не пройдена для {Uri}", uri);
            throw;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var tModel = await JsonSerializer.DeserializeAsync<T>(stream, CachedJsonSerializerOptions, cancellationToken);
        
        return tModel ?? throw new JsonException($"Не удалось десериализовать объект типа {typeof(T).Name} из {uri}");
    }
}
