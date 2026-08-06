using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SentinelFleet.Domain.Telemetry;

namespace SentinelFleet.Infrastructure.Persistence.Configurations;

public sealed class TelemetryEventConfiguration : IEntityTypeConfiguration<TelemetryEvent>
{
    public void Configure(EntityTypeBuilder<TelemetryEvent> builder)
    {
        builder.ToTable("telemetry_events");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.EventId).HasMaxLength(64).IsRequired();
        builder.Property(e => e.EventType).HasMaxLength(50).IsRequired();
        builder.Property(e => e.RawPayload).HasColumnType("jsonb");

        builder.HasIndex(e => e.EventId).IsUnique();
        builder.HasIndex(e => new { e.OrganizationId, e.AssetId, e.RecordedAt });
        builder.HasIndex(e => e.DeviceId);
    }
}
