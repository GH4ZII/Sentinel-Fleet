using SentinelFleet.Application.Telemetry;

namespace SentinelFleet.Application.Anomaly;

public interface IAnomalyEvaluator
{
    Task EvaluateTelemetryAsync(
        QueuedTelemetryMessage message,
        CancellationToken cancellationToken = default);
}
