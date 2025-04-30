using Mapster;
using PackageUniverse.Application.Models;
using PackageUniverse.Application.Models.NuGetModels;
using PackageUniverse.Core.Entities;

namespace PackageUniverse.Application.Mapping.Configuration;

public static class PackageUniverseMappingConfiguration
{
    public static void Configure(TypeAdapterConfig config)
    {
        config.NewConfig<PackageDetailModel, Package>()
            .Map(dest => dest.NugetId, src => src.PackageId)
            .Map(dest => dest.Description, src => src.Description)
            .Map(dest => dest.IsRecommended, _ => false) // по умолчанию
            .Ignore(dest => dest.Id); // ключ БД

        config.NewConfig<PackageDetailModel, PackageVersion>()
            .Map(dest => dest.Version, src => src.Version)
            .Map(dest => dest.PublishedAt, src => src.Published)
            .Map(dest => dest.IsPrerelease, src => src.IsPrerelease)
            .Map(dest => dest.PackageUrl, src => src.NuGetUri)
            .Map(dest => dest.TargetFramework, _ => "") // можно улучшить позже
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.PackageId)
            .Ignore(dest => dest.Dependencies);

        config.NewConfig<Dependency, PackageDependency>()
            .Map(dest => dest.TargetPackageId, _ => 0) // пока не мапим, установим вручную
            .Map(dest => dest.TargetVersionRange, src => src.Range)
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.SourceVersionId)
            .Ignore(dest => dest.TargetPackage);


        config.NewConfig<Package, PackageModel>()
            .Map(dest => dest.Name, src => src.NugetId)
            .Map(dest => dest.Dependencies,
                src => src.Versions
                    .OrderByDescending(v => v.PublishedAt)
                    .FirstOrDefault() != null
                    ? src.Versions
                        .OrderByDescending(v => v.PublishedAt)
                        .First().Dependencies
                    : new List<PackageDependency>())
            .Map(dest => dest.Versions, src => src.Versions);

        config.NewConfig<PackageVersion, PackageVersionModel>();

        config.NewConfig<PackageDependency, PackageDependencyModel>()
            .Map(dest => dest.TargetPackageName, src => src.TargetPackage.NugetId);
    }
}