using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SentinelFleet.Domain.Incidents;

namespace SentinelFleet.Infrastructure.Persistence.Configurations;

public sealed class IncidentTimelineEntryConfiguration : IEntityTypeConfiguration<IncidentTimelineEntry>
{
    public void Configure(EntityTypeBuilder<IncidentTimelineEntry> builder)
    {
        builder.ToTable("incident_timeline_entries");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Title).HasMaxLength(300).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(4000);
        builder.Property(e => e.EntryType).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(e => e.SourceType).HasMaxLength(100);
        builder.Property(e => e.Metadata).HasColumnType("jsonb");

        builder.HasIndex(e => e.IncidentId);
        builder.HasIndex(e => new { e.IncidentId, e.Timestamp });
        builder.HasIndex(e => e.OrganizationId);
    }
}
