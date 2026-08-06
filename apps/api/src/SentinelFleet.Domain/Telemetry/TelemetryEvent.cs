namespace SentinelFleet.Domain.Telemetry;

public sealed class TelemetryEvent
{
    public Guid Id { get; set; }

    /// <summary>Client-supplied unique event id (idempotency key).</summary>
    public required string EventId { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid AssetId { get; set; }

    public Guid DeviceId { get; set; }

    public required string EventType { get; set; }

    public DateTimeOffset RecordedAt { get; set; }

    public DateTimeOffset ReceivedAt { get; set; }

    public int SchemaVersion { get; set; } = 1;

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public double? SpeedKph { get; set; }

    public double? Heading { get; set; }

    public bool? IgnitionOn { get; set; }

    public double? OdometerKm { get; set; }

    public double? FuelLevelPercent { get; set; }

    public Guid? DriverUserId { get; set; }

    public string? RawPayload { get; set; }
}
