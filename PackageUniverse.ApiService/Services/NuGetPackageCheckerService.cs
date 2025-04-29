using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using PackageUniverse.ApiService.Validators;
using PackageUniverse.Application.Interfaces;
using PackageUniverse.Application.Models;
using PackageUniverse.Application.Models.NuGetModels;

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

    private IPUContext? _context;
    private HttpResponseValidationPipeline? _pipeline;


    private string NuGetGetUri => configuration["NuGet:CatalogsUri"] ??
                                  throw new InvalidOperationException("NuGet Catalog URI is not configured.");


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("NuGetPackageCheckerService running at: {Time}", DateTimeOffset.Now);

        using var scope = serviceProvider.CreateScope();

        _context = scope.ServiceProvider.GetRequiredService<IPUContext>();
        _pipeline = scope.ServiceProvider.GetRequiredService<HttpResponseValidationPipeline>();

        await CheckForNewPackagesAsync();

        //await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
    }


    private async Task<T> GetFromJson<T>(string uri) where T : Model
    {
        if (string.IsNullOrWhiteSpace(uri))
            throw new ArgumentException("Параметр URI не может быть пустым.", nameof(uri));
        if (_pipeline is null)
            throw new InvalidOperationException("Validation pipeline is not initialized");

        var response = await httpClient.GetAsync(uri);

        await _pipeline.ValidateAsync(
            new HttpValidationContext(response, uri),
            [HttpValidationTag.ExpectBody, HttpValidationTag.Get]
        );

        await using var stream = await response.Content.ReadAsStreamAsync();

        var tModel = await JsonSerializer.DeserializeAsync<T>(stream, CachedJsonSerializerOptions);

        return tModel ?? throw new JsonException($"Не удалось десериализовать объект типа {typeof(T).Name} из {uri}");
    }


    private async Task CheckForNewPackagesAsync()
    {
        if (_context is null)
            throw new InvalidOperationException("Context is not initialized");

        var catalogList = await GetFromJson<CatalogListModel>(NuGetGetUri);

        List<PackageDetailModel> detailPackages = new();
        ConcurrentBag<CatalogModel> concurrentCatalogModels = new();
        ConcurrentBag<PackageDetailModel> concurrentDetailPackages = new();

        // Обработка CatalogList.Items с использованием Parallel.ForEach
        ParallelOptions parallelOptions = new()
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount * 2 // Оптимальное количество потоков
        };

        try
        {
            // Обработка каталогов
            Parallel.ForEach(catalogList.Items, parallelOptions, catalog =>
            {
                try
                {
                    var catalogModel = GetFromJson<CatalogModel>(catalog.Id).Result;
                    concurrentCatalogModels.Add(catalogModel);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Ошибка при обработке каталога: {CatalogId}", catalog.Id);
                }
            });
            await using StreamWriter sw = new("C:\\Users\\diego\\Desktop\\catalogs.txt");
            foreach (var catalogModel in concurrentCatalogModels) await sw.WriteLineAsync(catalogModel.Id);

            // Обработка пакетов
            Parallel.ForEach(concurrentCatalogModels, parallelOptions, catalogModel =>
            {
                foreach (var package in catalogModel.Items)
                    try
                    {
                        var packageDetail = GetFromJson<PackageDetailModel>(package.Id).Result;
                        concurrentDetailPackages.Add(packageDetail);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Ошибка при обработке пакета: {PackageId}", catalogModel.Id);
                    }
            });

            // Перенос данных из ConcurrentBag в List
            detailPackages.AddRange(concurrentDetailPackages);
            await using StreamWriter sww = new("C:\\Users\\diego\\Desktop\\packages.txt");
            foreach (var package in detailPackages) await sww.WriteLineAsync(package.Id);
        }
        catch (AggregateException ex)
        {
            logger.LogError(ex, "Ошибка в процессе параллельной обработки.");
        }


        Debug.WriteLine("Получено {0} пакетов", detailPackages.Count);

        //foreach (var packageDetail in detailPackages)
        //{
        //        var newPackage = new Package
        //        {
        //            Name = packageDetail.CatalogEntry.Id,
        //            Description = packageDetail.CatalogEntry.Description,
        //            IsRecommended = false, 
        //        };
        //        if (packageDetail.CatalogEntry.DependencyGroups.Count > 0)
        //            newPackage.Dependencies = packageDetail.CatalogEntry.DependencyGroups.SelectMany(dg => dg.Dependencies).Select(d => new PackageDependency
        //            {
        //                Name = d.Id,
        //                Version = d.Range
        //            }).ToList();

        //        context.Packages.Add(newPackage);

        //}
        //await context.SaveChangesAsync();
        /////////////
        //var response = await _httpClient.GetAsync("https://api.nuget.org/v3/registration5-gz-semver2/newtonsoft.json/index.json");
        //if (response.IsSuccessStatusCode)
        //{
        //    var contentStream = await response.Content.ReadAsStreamAsync();
        //    using (var decompressedStream = new GZipStream(contentStream, CompressionMode.Decompress))
        //    using (var reader = new StreamReader(decompressedStream))
        //    {
        //        var content = await reader.ReadToEndAsync();
        //        var packageData = JsonSerializer.Deserialize<NuGetPackageData>(content, new JsonSerializerOptions
        //        {
        //            PropertyNameCaseInsensitive = true
        //        });
        //        if (packageData == null)
        //        {
        //            _logger.LogError("Failed to deserialize NuGet package data");
        //            return;
        //        }
        //}
        //}
    }
}