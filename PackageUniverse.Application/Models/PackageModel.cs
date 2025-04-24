namespace PackageUniverse.Application.Models
{
    public class PackageModel : Model
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsRecommended { get; set; }
        public List<PackageDependencyModel> Dependencies { get; set; } = new();

    }
}