using Microsoft.EntityFrameworkCore;
using PackageUniverse.Application.Interfaces;
using PackageUniverse.Core.Entities;
using PackageUniverse.Infrastructure.Exceptions;

namespace PackageUniverse.Infrastructure.Data;

public sealed class PUContext(DbContextOptions options)
    : DbContext(options), IPUContext
{
    public DbSet<PackageDependency> PackageDependencies { get; set; } = null!;
    public DbSet<Package> Packages { get; set; } = null!;
    public DbSet<PackageVersion> PackageVersions { get; set; } = null!;

    public override int SaveChanges()
    {
        var entries = ChangeTracker
            .Entries()
            .Where(e => e.Entity is Entity && (
                e.State == EntityState.Added
                || e.State == EntityState.Modified));

        foreach (var entityEntry in entries)
        {
            ((Entity)entityEntry.Entity).DateUpdated = DateTime.UtcNow;

            if (entityEntry.State == EntityState.Added) ((Entity)entityEntry.Entity).DateCreated = DateTime.UtcNow;
        }

        try
        {
            return base.SaveChanges();
        }
        catch (DbUpdateException ex)
        {
            throw new InfrastructureException($"Failed to save in DataBase {ex.Message}");
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker
            .Entries()
            .Where(e => e.Entity is Entity && (
                e.State == EntityState.Added ||
                e.State == EntityState.Modified));

        foreach (var entityEntry in entries)
        {
            ((Entity)entityEntry.Entity).DateUpdated = DateTime.UtcNow;

            if (entityEntry.State == EntityState.Added) ((Entity)entityEntry.Entity).DateCreated = DateTime.UtcNow;
        }

        try
        {
            return base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new InfrastructureException($"Failed to save in DataBase {ex.Message}");
        }
        catch (DbUpdateException ex)
        {
            throw new InfrastructureException($"Failed to save in DataBase {ex.Message}");
        }
        catch (OperationCanceledException ex)
        {
            throw new InfrastructureException($"Failed to save in DataBase {ex.Message}");
        }
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfigurationsFromAssembly(GetType().Assembly);
        base.OnModelCreating(builder);
    }
}