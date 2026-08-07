using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SentinelFleet.Domain.Drivers;

namespace SentinelFleet.Infrastructure.Persistence.Configurations;

public sealed class DriverAssignmentConfiguration : IEntityTypeConfiguration<DriverAssignment>
{
    public void Configure(EntityTypeBuilder<DriverAssignment> builder)
    {
        builder.ToTable("driver_assignments");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.AssignmentType).HasMaxLength(50).IsRequired();

        builder.HasIndex(d => d.OrganizationId);
        builder.HasIndex(d => new { d.AssetId, d.UserId, d.ValidFrom });
    }
}
