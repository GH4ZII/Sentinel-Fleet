using NetTopologySuite.Geometries;

namespace SentinelFleet.Domain.Geofences;

public sealed class Geofence
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    /// <summary>Polygon in WGS84 (SRID 4326). Coordinates are (X=lon, Y=lat).</summary>
    public required Polygon Geometry { get; set; }

    public GeofenceType GeofenceType { get; set; } = GeofenceType.Allowed;

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
