using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SentinelFleet.Domain.Assets;

namespace SentinelFleet.Infrastructure.Persistence.Configurations;

public sealed class AssetConfiguration : IEntityTypeConfiguration<Asset>
{
    public void Configure(EntityTypeBuilder<Asset> builder)
    {
        builder.ToTable("assets");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Name).HasMaxLength(200).IsRequired();
        builder.Property(a => a.AssetNumber).HasMaxLength(100);
        builder.Property(a => a.RegistrationNumber).HasMaxLength(50);
        builder.Property(a => a.SerialNumber).HasMaxLength(100);
        builder.Property(a => a.Manufacturer).HasMaxLength(100);
        builder.Property(a => a.Model).HasMaxLength(100);
        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(a => a.Criticality).HasConversion<string>().HasMaxLength(50).IsRequired();

        builder.HasIndex(a => a.OrganizationId);
        builder.HasIndex(a => new { a.OrganizationId, a.AssetNumber });
    }
}
