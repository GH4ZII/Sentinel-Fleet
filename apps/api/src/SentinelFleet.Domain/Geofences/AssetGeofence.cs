namespace SentinelFleet.Domain.Geofences;

public sealed class AssetGeofence
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid AssetId { get; set; }

    public Guid GeofenceId { get; set; }

    public Geofence Geofence { get; set; } = null!;

    public AssetGeofenceRuleType RuleType { get; set; } = AssetGeofenceRuleType.Both;

    public DateTimeOffset? ValidFrom { get; set; }

    public DateTimeOffset? ValidTo { get; set; }
}
