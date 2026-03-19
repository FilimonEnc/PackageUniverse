namespace PackageUniverse.Core.Entities;

public class DependencyGroup : Entity
{
    public int PackageDetailId { get; set; }

    public string NuGetUri { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string TargetFramework { get; set; } = string.Empty;

    public PackageDetail PackageDetail { get; set; } = null!;

    public ICollection<PackageDependencyDetail> Dependencies { get; set; } = new List<PackageDependencyDetail>();
}

