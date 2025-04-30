namespace PackageUniverse.Application.Models;

public class PackageDependencyModel : Model
{
    public string TargetPackageName { get; set; } = string.Empty; // NugetId из связанного Package
    public string TargetVersionRange { get; set; } = string.Empty;

    public string TargetFramework { get; set; } = string.Empty;
}