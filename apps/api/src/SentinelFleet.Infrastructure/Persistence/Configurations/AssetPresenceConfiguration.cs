using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SentinelFleet.Domain.Geofences;

namespace SentinelFleet.Infrastructure.Persistence.Configurations;

public sealed class AssetPresenceConfiguration : IEntityTypeConfiguration<AssetPresence>
{
    public void Configure(EntityTypeBuilder<AssetPresence> builder)
    {
        builder.ToTable("asset_presences");
        builder.HasKey(a => a.Id);

        builder.HasIndex(a => a.OrganizationId);
        builder.HasIndex(a => new { a.AssetId, a.GeofenceId }).IsUnique();
    }
}
