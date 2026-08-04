namespace SentinelFleet.Domain.Assets;

public sealed class Asset
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid AssetTypeId { get; set; }

    public AssetType AssetType { get; set; } = null!;

    public required string Name { get; set; }

    public string? AssetNumber { get; set; }

    public string? RegistrationNumber { get; set; }

    public string? SerialNumber { get; set; }

    public string? Manufacturer { get; set; }

    public string? Model { get; set; }

    public AssetStatus Status { get; set; } = AssetStatus.Active;

    public AssetCriticality Criticality { get; set; } = AssetCriticality.Medium;

    public Guid? CurrentUserId { get; set; }

    /// <summary>Placeholder map position until live telemetry arrives (Week 3).</summary>
    public double? MapLatitude { get; set; }

    /// <summary>Placeholder map position until live telemetry arrives (Week 3).</summary>
    public double? MapLongitude { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
