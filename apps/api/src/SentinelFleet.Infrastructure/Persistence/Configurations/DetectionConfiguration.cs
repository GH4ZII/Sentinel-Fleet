using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SentinelFleet.Domain.Detections;

namespace SentinelFleet.Infrastructure.Persistence.Configurations;

public sealed class DetectionConfiguration : IEntityTypeConfiguration<Detection>
{
    public void Configure(EntityTypeBuilder<Detection> builder)
    {
        builder.ToTable("detections");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Title).HasMaxLength(300).IsRequired();
        builder.Property(d => d.Description).HasMaxLength(4000);
        builder.Property(d => d.DetectionType).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(d => d.Severity).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(d => d.SourceEventIds).HasColumnType("jsonb");
        builder.Property(d => d.Metadata).HasColumnType("jsonb");

        builder.HasIndex(d => d.OrganizationId);
        builder.HasIndex(d => new { d.OrganizationId, d.TriggeredAt });
        builder.HasIndex(d => new { d.AssetId, d.DetectionType, d.TriggeredAt });
    }
}
