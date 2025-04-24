using PackageUniverse.Core.Entities;

public class PackageDependency : Entity
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
}
