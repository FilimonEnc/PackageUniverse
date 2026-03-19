namespace PackageUniverse.Core.Entities;

public class PackageEntry : Entity
{
    public int PackageDetailId { get; set; }

    public string NuGetUri { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public int CompressedLength { get; set; }

    public string FullName { get; set; } = string.Empty;

    public int Length { get; set; }

    public string Name { get; set; } = string.Empty;

    public PackageDetail PackageDetail { get; set; } = null!;
}

