namespace SentinelFleet.Application.Telemetry;

public sealed record TelemetryPositionDto(
    double Latitude,
    double Longitude,
    double? SpeedKph = null,
    double? Heading = null);

public sealed record TelemetryVehicleDto(
    bool? IgnitionOn = null,
    double? OdometerKm = null,
    double? FuelLevelPercent = null);

public sealed record TelemetryDriverDto(Guid? UserId = null);

public sealed record IngestTelemetryEventRequest(
    string EventId,
    DateTimeOffset RecordedAt,
    string EventType,
    TelemetryPositionDto Position,
    TelemetryVehicleDto? Vehicle = null,
    TelemetryDriverDto? Driver = null,
    int SchemaVersion = 1);

public sealed record IngestTelemetryBatchRequest(
    IReadOnlyList<IngestTelemetryEventRequest> Events);

public sealed record IngestTelemetryAcceptedResponse(
    string EventId,
    DateTimeOffset ReceivedAt);

public sealed record IngestTelemetryBatchResponse(
    IReadOnlyList<IngestTelemetryAcceptedResponse> Accepted,
    IReadOnlyList<IngestTelemetryBatchError> Rejected);

public sealed record IngestTelemetryBatchError(
    string? EventId,
    string Message);

public sealed record LatestTelemetryDto(
    Guid AssetId,
    Guid DeviceId,
    string EventId,
    string EventType,
    DateTimeOffset RecordedAt,
    DateTimeOffset ReceivedAt,
    double Latitude,
    double Longitude,
    double? SpeedKph,
    double? Heading,
    bool? IgnitionOn);

public sealed record AssetPositionDto(
    Guid AssetId,
    double Latitude,
    double Longitude,
    double? SpeedKph,
    double? Heading,
    DateTimeOffset RecordedAt);

public sealed record PositionUpdatedMessage(
    Guid OrganizationId,
    Guid AssetId,
    double Latitude,
    double Longitude,
    double? SpeedKph,
    double? Heading,
    DateTimeOffset RecordedAt);
