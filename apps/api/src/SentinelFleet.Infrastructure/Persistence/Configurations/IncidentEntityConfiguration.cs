using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SentinelFleet.Domain.Incidents;

namespace SentinelFleet.Infrastructure.Persistence.Configurations;

public sealed class IncidentEntityConfiguration : IEntityTypeConfiguration<IncidentEntity>
{
    public void Configure(EntityTypeBuilder<IncidentEntity> builder)
    {
        builder.ToTable("incident_entities");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.EntityType).HasMaxLength(100).IsRequired();
        builder.Property(e => e.RelationshipType).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Metadata).HasColumnType("jsonb");

        builder.HasIndex(e => e.IncidentId);
        builder.HasIndex(e => new { e.IncidentId, e.EntityType, e.EntityId }).IsUnique();
        builder.HasIndex(e => e.OrganizationId);
    }
}
