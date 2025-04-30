using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PackageUniverse.Core.Entities;

namespace PackageUniverse.Infrastructure.EntityTypeConfigurations;

public class PackageVersionConfiguration : IEntityTypeConfiguration<PackageVersion>
{
    public void Configure(EntityTypeBuilder<PackageVersion> builder)
    {
        builder.ToTable("PackageVersions");
        builder.HasKey(v => v.Id);

        builder.Property(v => v.Version)
            .IsRequired()
            .HasMaxLength(50);
        builder.Property(v => v.PublishedAt)
            .IsRequired();
        builder.Property(v => v.IsPrerelease)
            .IsRequired();
        builder.Property(v => v.TargetFramework)
            .HasMaxLength(100);
        builder.Property(v => v.PackageUrl)
            .IsRequired();

        builder.HasMany(v => v.Dependencies)
            .WithOne(d => d.SourceVersion)
            .HasForeignKey(d => d.SourceVersionId);
    }
}