using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SentinelFleet.Domain.Incidents;

namespace SentinelFleet.Infrastructure.Persistence.Configurations;

public sealed class RiskAssessmentConfiguration : IEntityTypeConfiguration<RiskAssessment>
{
    public void Configure(EntityTypeBuilder<RiskAssessment> builder)
    {
        builder.ToTable("risk_assessments");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.RiskLevel).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(r => r.Factors).HasColumnType("jsonb").IsRequired();
        builder.Property(r => r.ModelVersion).HasMaxLength(50).IsRequired();

        builder.HasIndex(r => r.IncidentId);
        builder.HasIndex(r => new { r.IncidentId, r.CalculatedAt });
        builder.HasIndex(r => r.OrganizationId);
    }
}
