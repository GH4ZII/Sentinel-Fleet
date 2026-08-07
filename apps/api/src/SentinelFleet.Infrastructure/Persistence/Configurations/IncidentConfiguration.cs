using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SentinelFleet.Domain.Incidents;

namespace SentinelFleet.Infrastructure.Persistence.Configurations;

public sealed class IncidentConfiguration : IEntityTypeConfiguration<Incident>
{
    public void Configure(EntityTypeBuilder<Incident> builder)
    {
        builder.ToTable("incidents");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Title).HasMaxLength(300).IsRequired();
        builder.Property(i => i.Description).HasMaxLength(4000);
        builder.Property(i => i.IncidentType).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(i => i.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(i => i.Severity).HasConversion<string>().HasMaxLength(50).IsRequired();

        builder.HasIndex(i => i.OrganizationId);
        builder.HasIndex(i => new { i.OrganizationId, i.Status });
        builder.HasIndex(i => new { i.OrganizationId, i.PrimaryAssetId, i.Status });
        builder.HasIndex(i => new { i.OrganizationId, i.DetectedAt });
    }
}
