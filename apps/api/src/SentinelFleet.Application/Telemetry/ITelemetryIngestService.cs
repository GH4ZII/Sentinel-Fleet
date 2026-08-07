namespace SentinelFleet.Application.Telemetry;

public interface ITelemetryIngestService
{
    Task<TelemetryResult<IngestTelemetryAcceptedResponse>> IngestAsync(
        string apiKey,
        IngestTelemetryEventRequest request,
        CancellationToken cancellationToken = default);

    Task<TelemetryResult<IngestTelemetryBatchResponse>> IngestBatchAsync(
        string apiKey,
        IngestTelemetryBatchRequest request,
        CancellationToken cancellationToken = default);
}

public interface ITelemetryQueryService
{
    Task<TelemetryResult<LatestTelemetryDto>> GetLatestForAssetAsync(
        Guid assetId,
        CancellationToken cancellationToken = default);

    Task<TelemetryResult<IReadOnlyList<AssetPositionDto>>> ListPositionsAsync(
        Guid assetId,
        int limit = 100,
        CancellationToken cancellationToken = default);

    Task<TelemetryResult<IReadOnlyList<LatestTelemetryDto>>> ListTelemetryAsync(
        Guid assetId,
        int limit = 100,
        CancellationToken cancellationToken = default);
}

public interface ITelemetryQueuePublisher
{
    Task PublishAsync(QueuedTelemetryMessage message, CancellationToken cancellationToken = default);
}

public interface ITelemetryProcessor
{
    Task ProcessAsync(QueuedTelemetryMessage message, CancellationToken cancellationToken = default);
}

public interface IFleetRealtimePublisher
{
    Task PublishPositionUpdatedAsync(
        PositionUpdatedMessage message,
        CancellationToken cancellationToken = default);

    Task PublishDetectionCreatedAsync(
        DetectionCreatedMessage message,
        CancellationToken cancellationToken = default);
}

/// <summary>Enriched message published to RabbitMQ after device validation.</summary>
public sealed record QueuedTelemetryMessage(
    string EventId,
    Guid OrganizationId,
    Guid AssetId,
    Guid DeviceId,
    string EventType,
    DateTimeOffset RecordedAt,
    DateTimeOffset ReceivedAt,
    int SchemaVersion,
    double Latitude,
    double Longitude,
    double? SpeedKph,
    double? Heading,
    bool? IgnitionOn,
    double? OdometerKm,
    double? FuelLevelPercent,
    Guid? DriverUserId,
    string RawPayload);

public class TelemetryResult
{
    public bool Succeeded { get; init; }

    public TelemetryError? Error { get; init; }

    public static TelemetryResult Success() => new() { Succeeded = true };

    public static TelemetryResult Failure(TelemetryError error) =>
        new() { Succeeded = false, Error = error };
}

public sealed class TelemetryResult<T> : TelemetryResult
{
    public T? Value { get; init; }

    public static TelemetryResult<T> Success(T value) =>
        new() { Succeeded = true, Value = value };

    public static new TelemetryResult<T> Failure(TelemetryError error) =>
        new() { Succeeded = false, Error = error };
}

public sealed record TelemetryError(TelemetryErrorCode Code, string Message);

public enum TelemetryErrorCode
{
    Validation,
    Unauthorized,
    NotFound,
    Forbidden
}
