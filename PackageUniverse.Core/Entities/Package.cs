using PackageUniverse.Core.Entities;
using System.ComponentModel.DataAnnotations;

public class Package : Entity
{
    [Required]
    public string PackageId { get; set; } = string.Empty;

    [Required]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public List<PackageDependency> Dependencies { get; set; } = new();
}
