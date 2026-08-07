using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SentinelFleet.Domain.Incidents;

namespace SentinelFleet.Infrastructure.Persistence.Configurations;

public sealed class IncidentAttachmentConfiguration : IEntityTypeConfiguration<IncidentAttachment>
{
    public void Configure(EntityTypeBuilder<IncidentAttachment> builder)
    {
        builder.ToTable("incident_attachments");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Name).HasMaxLength(300).IsRequired();
        builder.Property(a => a.ContentType).HasMaxLength(200).IsRequired();
        builder.Property(a => a.StorageKey).HasMaxLength(500).IsRequired();

        builder.HasIndex(a => a.IncidentId);
        builder.HasIndex(a => a.OrganizationId);
        builder.HasIndex(a => a.StorageKey).IsUnique();
    }
}
