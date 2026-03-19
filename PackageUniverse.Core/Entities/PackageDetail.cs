namespace PackageUniverse.Core.Entities;

public class PackageDetail : Entity
{
    public string NuGetUri { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string CatalogCommitId { get; set; } = string.Empty;

    public DateTime CatalogCommitTimeStamp { get; set; }

    public string Authors { get; set; } = string.Empty;

    public string Copyright { get; set; } = string.Empty;

    public DateTime Created { get; set; }

    public string Description { get; set; } = string.Empty;

    public string IconUrl { get; set; } = string.Empty;

    public string PackageId { get; set; } = string.Empty;

    public bool IsPrerelease { get; set; }

    public DateTime LastEdited { get; set; }

    public string LicenseUrl { get; set; } = string.Empty;

    public bool Listed { get; set; }

    public string PackageHash { get; set; } = string.Empty;

    public string PackageHashAlgorithm { get; set; } = string.Empty;

    public int PackageSize { get; set; }

    public string ProjectUrl { get; set; } = string.Empty;

    public DateTime Published { get; set; }

    public bool RequireLicenseAcceptance { get; set; }

    public string Serviceable { get; set; } = string.Empty;

    public string VerbatimVersion { get; set; } = string.Empty;

    public string Version { get; set; } = string.Empty;

    public ICollection<DependencyGroup> DependencyGroups { get; set; } = new List<DependencyGroup>();

    public ICollection<PackageEntry> PackageEntries { get; set; } = new List<PackageEntry>();

    public List<string> Tags { get; set; } = new();
}

