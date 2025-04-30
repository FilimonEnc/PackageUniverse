namespace PackageUniverse.Core.Entities;

public class Package : Entity
{
    public string NugetId { get; set; } = string.Empty; // хранит JSON-поле "@id" или "nuget:id"
    public string? Description { get; set; }
    public bool IsRecommended { get; set; }

    public ICollection<PackageVersion> Versions { get; set; } = new List<PackageVersion>();
}