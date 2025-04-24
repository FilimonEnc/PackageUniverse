using Mapster;

using PackageUniverse.Application.Models;

namespace PackageUniverse.Application.Mapping.Configuration
{
    public static class PackageUniverseMappingConfiguration
    {
        public static void Configure(TypeAdapterConfig config)
        {
            config.NewConfig<Package, PackageModel>()
                .Map(dest => dest.Dependencies, s => s.Dependencies != null);

        }


    }
}
