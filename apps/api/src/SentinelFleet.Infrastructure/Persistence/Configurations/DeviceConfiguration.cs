using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SentinelFleet.Domain.Devices;

namespace SentinelFleet.Infrastructure.Persistence.Configurations;

public sealed class DeviceConfiguration : IEntityTypeConfiguration<Device>
{
    public void Configure(EntityTypeBuilder<Device> builder)
    {
        builder.ToTable("devices");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.ExternalDeviceId).HasMaxLength(100).IsRequired();
        builder.Property(d => d.DeviceType).HasMaxLength(100).IsRequired();
        builder.Property(d => d.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(d => d.FirmwareVersion).HasMaxLength(50);
        builder.Property(d => d.ApiKeyHash).HasMaxLength(128).IsRequired();

        builder.HasIndex(d => d.OrganizationId);
        builder.HasIndex(d => new { d.OrganizationId, d.ExternalDeviceId }).IsUnique();
        builder.HasIndex(d => d.ApiKeyHash).IsUnique();
    }
}
