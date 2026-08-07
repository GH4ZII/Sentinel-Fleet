namespace SentinelFleet.Application.Anomaly;

public sealed record AnomalyFeatureVector(
    double HourOfDay,
    double DayOfWeek,
    double SpeedKph,
    double IgnitionOn,
    double? FuelLevelPercent,
    double? OdometerKm);

public sealed record AnomalyScoreRequest(
    Guid OrganizationId,
    Guid AssetId,
    string? EventId,
    DateTimeOffset? RecordedAt,
    AnomalyFeatureVector Features);

public sealed record AnomalyScoreResult(
    double AnomalyScore,
    double Confidence,
    string ModelVersion,
    IReadOnlyList<string> FeaturesUsed,
    string Explanation,
    bool IsAnomaly,
    string Method);

public interface IAnomalyServiceClient
{
    Task<AnomalyScoreResult?> ScoreAsync(
        AnomalyScoreRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
}
