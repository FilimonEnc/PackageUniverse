using System.Text.Json.Serialization;

namespace PackageUniverse.Application.Models.NuGetModels;

#region Список страниц

public class CatalogListModel : Model
{
    [JsonPropertyName("@id")] public string Id { get; set; } = string.Empty;

    [JsonPropertyName("@type")] public List<string> Type { get; set; } = new();

    [JsonPropertyName("commitId")] public string CommitId { get; set; } = string.Empty;

    [JsonPropertyName("commitTimeStamp")] public DateTime CommitTimeStamp { get; set; }

    [JsonPropertyName("count")] public int Count { get; set; }

    [JsonPropertyName("nuget:lastCreated")]
    public DateTime LastCreated { get; set; }

    [JsonPropertyName("nuget:lastDeleted")]
    public DateTime LastDeleted { get; set; }

    [JsonPropertyName("nuget:lastEdited")] public DateTime LastEdited { get; set; }

    [JsonPropertyName("items")] public List<CatalogPage> Items { get; set; } = new();
}

public class CatalogPage : Model
{
    [JsonPropertyName("@id")] public string Id { get; set; } = string.Empty;

    [JsonPropertyName("@type")] public string Type { get; set; } = string.Empty;

    [JsonPropertyName("commitId")] public string CommitId { get; set; } = string.Empty;

    [JsonPropertyName("commitTimeStamp")] public DateTime CommitTimeStamp { get; set; }

    [JsonPropertyName("count")] public int Count { get; set; }
}

#endregion

#region Список пакетов на странице

public class CatalogModel : Model
{
    [JsonPropertyName("@id")] public string Id { get; set; } = string.Empty;

    [JsonPropertyName("@type")] public string Type { get; set; } = string.Empty;

    [JsonPropertyName("commitId")] public string CommitId { get; set; } = string.Empty;

    [JsonPropertyName("commitTimeStamp")] public DateTime CommitTimeStamp { get; set; }

    [JsonPropertyName("count")] public int Count { get; set; }

    [JsonPropertyName("parent")] public string Parent { get; set; } = string.Empty;

    [JsonPropertyName("items")] public List<PackageDetail> Items { get; set; } = new();
}

public class PackageDetail : Model
{
    [JsonPropertyName("@id")] public string Id { get; set; } = string.Empty;

    [JsonPropertyName("@type")] public string Type { get; set; } = string.Empty;

    [JsonPropertyName("commitId")] public string CommitId { get; set; } = string.Empty;

    [JsonPropertyName("commitTimeStamp")] public DateTime CommitTimeStamp { get; set; }

    [JsonPropertyName("nuget:id")] public string NugetId { get; set; } = string.Empty;

    [JsonPropertyName("nuget:version")] public string NugetVersion { get; set; } = string.Empty;
}

#endregion

#region Детализация пакета

public class PackageDetailModel : Model
{
    [JsonPropertyName("@id")] public string Id { get; set; } = string.Empty;

    [JsonPropertyName("@type")] public List<string> Type { get; set; } = new();

    [JsonPropertyName("authors")] public string Authors { get; set; } = string.Empty;

    [JsonPropertyName("catalog:commitId")] public string CatalogCommitId { get; set; } = string.Empty;

    [JsonPropertyName("catalog:commitTimeStamp")]
    public DateTime CatalogCommitTimeStamp { get; set; }

    [JsonPropertyName("copyright")] public string Copyright { get; set; } = string.Empty;

    [JsonPropertyName("created")] public DateTime Created { get; set; }

    [JsonPropertyName("description")] public string Description { get; set; } = string.Empty;

    [JsonPropertyName("iconUrl")] public string IconUrl { get; set; } = string.Empty;

    [JsonPropertyName("id")] public string PackageId { get; set; } = string.Empty;

    [JsonPropertyName("isPrerelease")] public bool IsPrerelease { get; set; }

    [JsonPropertyName("lastEdited")] public DateTime LastEdited { get; set; }

    [JsonPropertyName("licenseUrl")] public string LicenseUrl { get; set; } = string.Empty;

    [JsonPropertyName("listed")] public bool Listed { get; set; }

    [JsonPropertyName("packageHash")] public string PackageHash { get; set; } = string.Empty;

    [JsonPropertyName("packageHashAlgorithm")]
    public string PackageHashAlgorithm { get; set; } = string.Empty;

    [JsonPropertyName("packageSize")] public int PackageSize { get; set; }

    [JsonPropertyName("projectUrl")] public string ProjectUrl { get; set; } = string.Empty;

    [JsonPropertyName("published")] public DateTime Published { get; set; }

    [JsonPropertyName("requireLicenseAcceptance")]
    public bool RequireLicenseAcceptance { get; set; }

    [JsonPropertyName("serviceable")] public string Serviceable { get; set; } = string.Empty;

    [JsonPropertyName("verbatimVersion")] public string VerbatimVersion { get; set; } = string.Empty;

    [JsonPropertyName("version")] public string Version { get; set; } = string.Empty;

    [JsonPropertyName("dependencyGroups")] public List<DependencyGroup> DependencyGroups { get; set; } = new();

    [JsonPropertyName("packageEntries")] public List<PackageEntry> PackageEntries { get; set; } = new();

    [JsonPropertyName("tags")] public List<string> Tags { get; set; } = new();
}

public class DependencyGroup : Model
{
    [JsonPropertyName("@id")] public string Id { get; set; } = string.Empty;

    [JsonPropertyName("@type")] public string Type { get; set; } = string.Empty;

    [JsonPropertyName("dependencies")] public List<Dependency> Dependencies { get; set; } = new();

    [JsonPropertyName("targetFramework")] public string TargetFramework { get; set; } = string.Empty;
}

public class Dependency : Model
{
    [JsonPropertyName("@id")] public string Id { get; set; } = string.Empty;

    [JsonPropertyName("@type")] public string Type { get; set; } = string.Empty;

    [JsonPropertyName("id")] public string DependencyId { get; set; } = string.Empty;

    [JsonPropertyName("range")] public string Range { get; set; } = string.Empty;
}

public class PackageEntry : Model
{
    [JsonPropertyName("@id")] public string Id { get; set; } = string.Empty;

    [JsonPropertyName("@type")] public string Type { get; set; } = string.Empty;

    [JsonPropertyName("compressedLength")] public int CompressedLength { get; set; }

    [JsonPropertyName("fullName")] public string FullName { get; set; } = string.Empty;

    [JsonPropertyName("length")] public int Length { get; set; }

    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
}

#endregion