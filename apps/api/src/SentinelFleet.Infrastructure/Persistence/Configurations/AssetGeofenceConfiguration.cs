using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SentinelFleet.Domain.Geofences;

namespace SentinelFleet.Infrastructure.Persistence.Configurations;

public sealed class AssetGeofenceConfiguration : IEntityTypeConfiguration<AssetGeofence>
{
    public void Configure(EntityTypeBuilder<AssetGeofence> builder)
    {
        builder.ToTable("asset_geofences");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.RuleType).HasConversion<string>().HasMaxLength(50).IsRequired();

        builder.HasOne(a => a.Geofence)
            .WithMany()
            .HasForeignKey(a => a.GeofenceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(a => a.OrganizationId);
        builder.HasIndex(a => new { a.AssetId, a.GeofenceId }).IsUnique();
    }
}
