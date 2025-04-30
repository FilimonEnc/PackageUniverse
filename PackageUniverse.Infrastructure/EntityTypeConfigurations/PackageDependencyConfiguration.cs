using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PackageUniverse.Core.Entities;

namespace PackageUniverse.Infrastructure.EntityTypeConfigurations;

public class PackageDependencyConfiguration : IEntityTypeConfiguration<PackageDependency>
{
    public void Configure(EntityTypeBuilder<PackageDependency> builder)
    {
        builder.ToTable("PackageDependencies");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.TargetVersionRange)
            .IsRequired()
            .HasMaxLength(100);
        builder.Property(d => d.TargetFramework)
            .HasMaxLength(100);

        builder.HasOne(d => d.SourceVersion)
            .WithMany(v => v.Dependencies)
            .HasForeignKey(d => d.SourceVersionId);

        builder.HasOne(d => d.TargetPackage)
            .WithMany()
            .HasForeignKey(d => d.TargetPackageId);
    }
}