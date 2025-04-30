namespace PackageUniverse.Core.Entities;

public class PackageDependency : Entity
{
    public int SourceVersionId { get; set; }
    public int TargetPackageId { get; set; }
    public string TargetVersionRange { get; set; } = string.Empty;
    public string TargetFramework { get; set; } = string.Empty;

    public PackageVersion SourceVersion { get; set; } = null!;
    public Package TargetPackage { get; set; } = null!;
}