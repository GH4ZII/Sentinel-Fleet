using SentinelFleet.Domain.Detections;
using SentinelFleet.Domain.Incidents;

namespace SentinelFleet.Application.Incidents;

public interface IIncidentCorrelator
{
    /// <summary>
    /// Correlate newly persisted detections into open incidents (or create new ones),
    /// recalculate risk, and publish realtime updates.
    /// </summary>
    Task CorrelateAsync(
        IReadOnlyList<Detection> detections,
        CancellationToken cancellationToken = default);
}

public interface IRiskScoringService
{
    Task<RiskAssessment> RecalculateAsync(
        Guid incidentId,
        CancellationToken cancellationToken = default);
}
