namespace PackageUniverse.Application.Models;

public class PackageVersionModel : Model
{
    public string Version { get; set; } = string.Empty;
    public DateTime PublishedAt { get; set; }
    public bool IsPrerelease { get; set; }
    public string TargetFramework { get; set; } = string.Empty;

    public bool IsRecommended { get; set; }

    public string PackageUrl { get; set; } = string.Empty;

    public List<PackageDependencyModel> Dependencies { get; set; } = new();
}