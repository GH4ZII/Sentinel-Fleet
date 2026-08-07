namespace SentinelFleet.Domain.Geofences;

/// <summary>Tracks whether an asset was last observed inside a geofence (for enter/exit).</summary>
public sealed class AssetPresence
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid AssetId { get; set; }

    public Guid GeofenceId { get; set; }

    public bool IsInside { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
