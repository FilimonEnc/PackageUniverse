using PackageUniverse.ApiService.Controllers;
using PackageUniverse.Application.Interfaces;
using PackageUniverse.Application.Models;
using PackageUniverse.Application.Models.NuGetModels;

using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text.Json;

namespace PackageUniverse.ApiService.Services
{
    public class NuGetPackageCheckerService : BackgroundService
    {
        private readonly ILogger<NuGetPackageCheckerService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly HttpClient _httpClient;

        private const string NUGET_GET_URI = "https://api.nuget.org/v3/catalog0/index.json";

        CatalogListModel? CatalogList = null;


        public NuGetPackageCheckerService(ILogger<NuGetPackageCheckerService> logger, IServiceProvider serviceProvider, HttpClient httpClient)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _httpClient = httpClient;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //while (!stoppingToken.IsCancellationRequested)

            //{
            _logger.LogInformation("NuGetPackageCheckerService running at: {time}", DateTimeOffset.Now);

            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<IPUContext>();

                await CheckForNewPackagesAsync(context);
            }

            // Ожидание 24 часа перед следующей проверкой
            await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
            //}
        }

        private async Task<T> GetFromJSON<T>(string uri) where T : Model
        {
            var response = await _httpClient.GetAsync(uri);
            if (!response.IsSuccessStatusCode)
                throw new Exception("Не удалось получить данные");

            T? tModel;

            using (var stream = await response.Content.ReadAsStreamAsync())
            {
                var reader = new StreamReader(stream);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                tModel = await JsonSerializer.DeserializeAsync<T>(stream, options);
            }

            return tModel!;
        }

        private async Task CheckForNewPackagesAsync(IPUContext context)
        {
            CatalogList = await GetFromJSON<CatalogListModel>(NUGET_GET_URI);

            List<CatalogModel> catalogModels = new();
            List<PackageDetailModel> detailPackages = new();
            object lockObject = new(); // Для синхронизации доступа к общим ресурсам

            // Обработка CatalogList.Items с использованием 500 потоков
            ParallelOptions parallelOptions = new()
            {
                MaxDegreeOfParallelism = 10000, // Ограничение на количество потоков
            };

            await Task.Run(() =>
            {
                Parallel.ForEach(CatalogList.Items, parallelOptions, catalog =>
                {
                    try
                    {
                        var catalogModel = GetFromJSON<CatalogModel>(catalog.Id).Result;

                        lock (lockObject)
                        {
                            catalogModels.Add(catalogModel);
                        }

                        
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Ошибка при обработке каталога: {CatalogId}", catalog.Id);
                    }
                });
                Parallel.ForEach(catalogModels, parallelOptions, package =>
                {
                    try
                    {
                        var packageDetail = GetFromJSON<PackageDetailModel>(package.Id).Result;

                        lock (lockObject)
                        {
                            detailPackages.Add(packageDetail);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Ошибка при обработке пакета: {PackageId}", package.Id);
                    }
                });
            });

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


