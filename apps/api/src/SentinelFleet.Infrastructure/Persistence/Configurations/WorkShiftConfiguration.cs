using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SentinelFleet.Domain.Drivers;

namespace SentinelFleet.Infrastructure.Persistence.Configurations;

public sealed class WorkShiftConfiguration : IEntityTypeConfiguration<WorkShift>
{
    public void Configure(EntityTypeBuilder<WorkShift> builder)
    {
        builder.ToTable("work_shifts");
        builder.HasKey(w => w.Id);

        builder.Property(w => w.Status).HasMaxLength(50).IsRequired();

        builder.HasIndex(w => w.OrganizationId);
        builder.HasIndex(w => new { w.UserId, w.StartsAt, w.EndsAt });
    }
}
