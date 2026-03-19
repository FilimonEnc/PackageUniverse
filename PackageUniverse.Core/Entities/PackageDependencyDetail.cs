namespace PackageUniverse.Core.Entities;

public class PackageDependencyDetail : Entity
{
    public int DependencyGroupId { get; set; }

    public string NuGetUri { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string DependencyId { get; set; } = string.Empty;

    public string Range { get; set; } = string.Empty;

    public DependencyGroup DependencyGroup { get; set; } = null!;
}

