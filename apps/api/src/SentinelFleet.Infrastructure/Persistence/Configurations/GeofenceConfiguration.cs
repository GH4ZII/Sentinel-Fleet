using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SentinelFleet.Domain.Geofences;

namespace SentinelFleet.Infrastructure.Persistence.Configurations;

public sealed class GeofenceConfiguration : IEntityTypeConfiguration<Geofence>
{
    public void Configure(EntityTypeBuilder<Geofence> builder)
    {
        builder.ToTable("geofences");
        builder.HasKey(g => g.Id);

        builder.Property(g => g.Name).HasMaxLength(200).IsRequired();
        builder.Property(g => g.Description).HasMaxLength(2000);
        builder.Property(g => g.GeofenceType).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(g => g.Geometry).HasColumnType("geometry(Polygon, 4326)").IsRequired();

        builder.HasIndex(g => g.OrganizationId);
        builder.HasIndex(g => g.Geometry).HasMethod("GIST");
    }
}
