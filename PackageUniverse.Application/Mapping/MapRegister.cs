using PackageUniverse.Application.Mapping.Configuration;

using Mapster;

namespace PackageUniverse.Application.Mapping
{
    public class MapRegister : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            PackageUniverseMappingConfiguration.Configure(config);
        }
    }
}