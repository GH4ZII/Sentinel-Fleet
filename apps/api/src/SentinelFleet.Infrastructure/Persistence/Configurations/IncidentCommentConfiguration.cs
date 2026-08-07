using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SentinelFleet.Domain.Incidents;

namespace SentinelFleet.Infrastructure.Persistence.Configurations;

public sealed class IncidentCommentConfiguration : IEntityTypeConfiguration<IncidentComment>
{
    public void Configure(EntityTypeBuilder<IncidentComment> builder)
    {
        builder.ToTable("incident_comments");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Content).HasMaxLength(4000).IsRequired();

        builder.HasIndex(c => c.IncidentId);
        builder.HasIndex(c => c.OrganizationId);
    }
}
