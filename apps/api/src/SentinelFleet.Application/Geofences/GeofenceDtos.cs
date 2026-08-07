using SentinelFleet.Domain.Geofences;

namespace SentinelFleet.Application.Geofences;

public sealed record CoordinateDto(double Longitude, double Latitude);

public sealed record PolygonGeometryDto(
    string Type,
    IReadOnlyList<IReadOnlyList<double[]>> Coordinates);

public sealed record GeofenceDto(
    Guid Id,
    string Name,
    string? Description,
    GeofenceType GeofenceType,
    bool IsActive,
    PolygonGeometryDto Geometry,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CreateGeofenceRequest(
    string Name,
    string? Description,
    GeofenceType GeofenceType,
    IReadOnlyList<CoordinateDto> Coordinates,
    bool IsActive = true);

public sealed record UpdateGeofenceRequest(
    string? Name,
    string? Description,
    GeofenceType? GeofenceType,
    IReadOnlyList<CoordinateDto>? Coordinates,
    bool? IsActive);

public sealed record AssetGeofenceDto(
    Guid Id,
    Guid AssetId,
    Guid GeofenceId,
    AssetGeofenceRuleType RuleType,
    DateTimeOffset? ValidFrom,
    DateTimeOffset? ValidTo);

public sealed record LinkAssetGeofenceRequest(
    Guid AssetId,
    AssetGeofenceRuleType RuleType = AssetGeofenceRuleType.Both,
    DateTimeOffset? ValidFrom = null,
    DateTimeOffset? ValidTo = null);
