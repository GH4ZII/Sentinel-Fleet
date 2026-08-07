using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SentinelFleet.Domain.Rules;

namespace SentinelFleet.Infrastructure.Persistence.Configurations;

public sealed class DetectionRuleConfiguration : IEntityTypeConfiguration<DetectionRule>
{
    public void Configure(EntityTypeBuilder<DetectionRule> builder)
    {
        builder.ToTable("detection_rules");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name).HasMaxLength(200).IsRequired();
        builder.Property(r => r.Description).HasMaxLength(2000);
        builder.Property(r => r.RuleType).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(r => r.Severity).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(r => r.Configuration).HasColumnType("jsonb");

        builder.HasIndex(r => r.OrganizationId);
        builder.HasIndex(r => new { r.OrganizationId, r.RuleType });
    }
}
