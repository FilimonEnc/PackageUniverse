using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PackageUniverse.Application.Interfaces;
using PackageUniverse.Application.Models;
using PackageUniverse.Core.Entities;

namespace PackageUniverse.ApiService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PackagesController : BaseController
{
    private readonly IPUContext _context;

    public PackagesController(IPUContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    ///     Returns all packages with their dependencies and version counts for bubble visualization
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<PackageModel>>> GetAll()
    {
        var packages = await _context.Packages
            .Include(p => p.Versions)
            .AsNoTracking()
            .ToListAsync();

        var dependencies = await _context.PackageDependencies
            .Include(d => d.TargetPackage)
            .AsNoTracking()
            .ToListAsync();

        var result = packages.Select(p => new PackageModel
        {
            Id = p.Id,
            Name = p.NugetId,
            Description = p.Description,
            IsRecommended = p.IsRecommended,
            TotalDownloads = p.Versions.Count, // Using version count as proxy for bubble size
            Versions = p.Versions
                .OrderByDescending(v => v.PublishedAt)
                .Select(v => new PackageVersionModel
                {
                    Id = v.Id,
                    Version = v.Version,
                    PublishedAt = v.PublishedAt,
                    IsPrerelease = v.IsPrerelease,
                    TargetFramework = v.TargetFramework,
                    PackageUrl = v.PackageUrl
                })
                .ToList(),
            Dependencies = dependencies
                .Where(d => _context.PackageVersions
                    .Any(pv => pv.PackageId == p.Id && pv.Id == d.SourceVersionId))
                .Select(d => new PackageDependencyModel
                {
                    TargetPackageName = d.TargetPackage.NugetId,
                    TargetVersionRange = d.TargetVersionRange,
                    TargetFramework = d.TargetFramework
                })
                .ToList()
        }).ToList();

        return Ok(result);
    }

    /// <summary>
    ///     Returns packages with dependency graph data for visualization
    /// </summary>
    [HttpGet("graph")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<PackageGraphResponse>> GetGraph()
    {
        var packages = await _context.Packages
            .Include(p => p.Versions)
            .AsNoTracking()
            .ToListAsync();

        var dependencies = await _context.PackageDependencies
            .Include(d => d.SourceVersion)
            .Include(d => d.TargetPackage)
            .AsNoTracking()
            .ToListAsync();

        var packageLookup = packages.ToDictionary(p => p.Id);
        var targetPackageLookup = packages.ToDictionary(p => p.NugetId);

        var edges = new List<PackageEdge>();
        var seenEdges = new HashSet<string>();

        foreach (var dep in dependencies)
        {
            if (!packageLookup.TryGetValue(dep.SourceVersion.PackageId, out var sourcePackage))
                continue;
            if (!targetPackageLookup.TryGetValue(dep.TargetPackage.NugetId, out var _))
                continue;

            var edgeKey = $"{sourcePackage.NugetId}->{dep.TargetPackage.NugetId}";
            if (seenEdges.Add(edgeKey))
            {
                edges.Add(new PackageEdge
                {
                    Source = sourcePackage.NugetId,
                    Target = dep.TargetPackage.NugetId,
                    VersionRange = dep.TargetVersionRange,
                    TargetFramework = dep.TargetFramework
                });
            }
        }

        var nodes = packages.Select(p => new PackageNode
        {
            Name = p.NugetId,
            Description = p.Description,
            Size = p.Versions.Count,
            IsRecommended = p.IsRecommended
        }).ToList();

        return Ok(new PackageGraphResponse
        {
            Nodes = nodes,
            Edges = edges
        });
    }
}

public record PackageGraphResponse
{
    public List<PackageNode> Nodes { get; init; } = [];
    public List<PackageEdge> Edges { get; init; } = [];
}

public record PackageNode
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int Size { get; init; }
    public bool IsRecommended { get; init; }
}

public record PackageEdge
{
    public string Source { get; init; } = string.Empty;
    public string Target { get; init; } = string.Empty;
    public string VersionRange { get; init; } = string.Empty;
    public string TargetFramework { get; init; } = string.Empty;
}