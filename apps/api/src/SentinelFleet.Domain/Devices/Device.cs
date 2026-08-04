namespace SentinelFleet.Domain.Devices;

public sealed class Device
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid? AssetId { get; set; }

    public required string ExternalDeviceId { get; set; }

    public required string DeviceType { get; set; }

    public DeviceStatus Status { get; set; } = DeviceStatus.Active;

    public DateTimeOffset? LastSeenAt { get; set; }

    public string? FirmwareVersion { get; set; }

    public required string ApiKeyHash { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
