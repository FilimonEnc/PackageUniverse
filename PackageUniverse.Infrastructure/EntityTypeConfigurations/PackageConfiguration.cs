using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PackageUniverse.Core.Entities;

namespace PackageUniverse.Infrastructure.EntityTypeConfigurations;

public class PackageConfiguration : IEntityTypeConfiguration<Package>
{
    public void Configure(EntityTypeBuilder<Package> builder)
    {
        builder.HasIndex(p => new { p.NugetId })
            .IsUnique(); // Уникальный индекс
        builder.ToTable("Packages");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Description)
            .HasMaxLength(1000);
        builder.Property(p => p.IsRecommended)
            .IsRequired();

        builder.HasMany(p => p.Versions)
            .WithOne(v => v.Package)
            .HasForeignKey(v => v.PackageId);
    }
}