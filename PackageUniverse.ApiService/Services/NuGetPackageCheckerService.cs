using System.Collections.Concurrent;
using PackageUniverse.Application.Interfaces;
using PackageUniverse.Application.Models;
using PackageUniverse.Application.Models.NuGetModels;

using System.Diagnostics;
using System.Text.Json;

namespace PackageUniverse.ApiService.Services
{
    public class NuGetPackageCheckerService(
        ILogger<NuGetPackageCheckerService> logger,
        IServiceProvider serviceProvider,
        HttpClient httpClient,
        IConfiguration configuration)
        : BackgroundService
    {

        private string NuGetGetUri => configuration["NuGet:CatalogsUri"] ?? throw new InvalidOperationException("NuGet Catalog URI is not configured.");

        private CatalogListModel? _catalogList = null;


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //while (!stoppingToken.IsCancellationRequested)

            //{
            logger.LogInformation("NuGetPackageCheckerService running at: {Time}", DateTimeOffset.Now);

            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<IPUContext>();

                await CheckForNewPackagesAsync(context);
            }

            // Ожидание 24 часа перед следующей проверкой
            await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
            //}
        }

        private static readonly JsonSerializerOptions CachedJsonSerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private async Task<T> GetFromJson<T>(string uri) where T : Model
        {
            var response = await httpClient.GetAsync(uri);
            if (!response.IsSuccessStatusCode)
                throw new ArgumentNullException(nameof(uri), "Не удалось получить данные");

            await using var stream = await response.Content.ReadAsStreamAsync();

            var tModel = await JsonSerializer.DeserializeAsync<T>(stream, CachedJsonSerializerOptions);

            return tModel ?? throw new ArgumentNullException(nameof(uri), "Не удалось десериализовать данные");
        }

        private async Task CheckForNewPackagesAsync(IPUContext context)
        {
            _catalogList = await GetFromJson<CatalogListModel>(NuGetGetUri);

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
                Parallel.ForEach(_catalogList.Items, parallelOptions, catalog =>
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

                // Обработка пакетов
                Parallel.ForEach(concurrentCatalogModels, parallelOptions, catalogModel =>
                {
                    try
                    {
                        var packageDetail = GetFromJson<PackageDetailModel>(catalogModel.Id).Result;
                        concurrentDetailPackages.Add(packageDetail);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Ошибка при обработке пакета: {PackageId}", catalogModel.Id);
                    }
                });

                // Перенос данных из ConcurrentBag в List
                detailPackages.AddRange(concurrentDetailPackages);
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
            //            IsRecommended = false, //TODO: Установите логику для определения рекомендуемых пакетов
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

            return;


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
        }
    }
}


