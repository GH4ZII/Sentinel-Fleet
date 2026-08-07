using SentinelFleet.Application.Telemetry;
using SentinelFleet.Domain.Detections;

namespace SentinelFleet.Application.Rules;

public interface IRuleEngine
{
    Task<IReadOnlyList<Detection>> EvaluateTelemetryAsync(
        QueuedTelemetryMessage current,
        CancellationToken cancellationToken = default);
}

public interface IGpsOfflineEvaluator
{
    Task EvaluateAsync(CancellationToken cancellationToken = default);
}
