using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SentinelFleet.Domain.Anomaly;

namespace SentinelFleet.Infrastructure.Persistence.Configurations;

public sealed class AnomalyAssessmentConfiguration : IEntityTypeConfiguration<AnomalyAssessment>
{
    public void Configure(EntityTypeBuilder<AnomalyAssessment> builder)
    {
        builder.ToTable("anomaly_assessments");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.ModelVersion).HasMaxLength(100).IsRequired();
        builder.Property(a => a.Method).HasMaxLength(50).IsRequired();
        builder.Property(a => a.Explanation).HasMaxLength(4000).IsRequired();
        builder.Property(a => a.FeaturesUsed).HasColumnType("jsonb");

        builder.HasIndex(a => a.OrganizationId);
        builder.HasIndex(a => new { a.AssetId, a.CalculatedAt });
        builder.HasIndex(a => a.IncidentId);
        builder.HasIndex(a => a.IsAnomaly);
    }
}
