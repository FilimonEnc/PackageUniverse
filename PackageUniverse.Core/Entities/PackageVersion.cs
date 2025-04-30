namespace PackageUniverse.Core.Entities;

public class PackageVersion : Entity
{
    public int PackageId { get; set; }
    public string Version { get; set; } = string.Empty;
    public DateTime PublishedAt { get; set; }
    public bool IsPrerelease { get; set; }
    public string TargetFramework { get; set; } = string.Empty;
    public string PackageUrl { get; set; } = string.Empty;

    public Package Package { get; set; } = null!;
    public ICollection<PackageDependency> Dependencies { get; set; } = new List<PackageDependency>();
}