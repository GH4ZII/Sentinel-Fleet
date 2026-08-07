using SentinelFleet.Domain.Detections;
using SentinelFleet.Domain.Incidents;
using SentinelFleet.Domain.Rules;

namespace SentinelFleet.Application.Incidents;

public static class IncidentCorrelation
{
    public static readonly TimeSpan CorrelationWindow = TimeSpan.FromMinutes(30);

    public static bool IsOpenStatus(IncidentStatus status) =>
        status is IncidentStatus.Open or IncidentStatus.Investigating;

    public static bool IsWithinWindow(DateTimeOffset lastActivityAt, DateTimeOffset detectionAt) =>
        detectionAt - lastActivityAt <= CorrelationWindow &&
        lastActivityAt - detectionAt <= CorrelationWindow;

    public static IncidentType MapIncidentType(DetectionRuleType detectionType) =>
        detectionType switch
        {
            DetectionRuleType.GeofenceEnter or DetectionRuleType.GeofenceExit =>
                IncidentType.GeofenceViolation,
            DetectionRuleType.UnauthorizedUser => IncidentType.UnauthorizedUse,
            DetectionRuleType.FuelLoss => IncidentType.FuelAnomaly,
            DetectionRuleType.GpsOffline => IncidentType.GpsAnomaly,
            DetectionRuleType.OutsideWorkHours => IncidentType.UnauthorizedUse,
            DetectionRuleType.UsageAnomaly => IncidentType.SuspiciousActivity,
            _ => IncidentType.SuspiciousActivity
        };

    public static IncidentSeverity MapSeverity(DetectionSeverity severity) =>
        severity switch
        {
            DetectionSeverity.Low => IncidentSeverity.Low,
            DetectionSeverity.Medium => IncidentSeverity.Medium,
            DetectionSeverity.High => IncidentSeverity.High,
            DetectionSeverity.Critical => IncidentSeverity.Critical,
            _ => IncidentSeverity.Medium
        };

    public static IncidentSeverity MaxSeverity(IncidentSeverity current, IncidentSeverity incoming) =>
        (IncidentSeverity)Math.Max((int)current, (int)incoming);

    public static string BuildTitle(IReadOnlyList<Detection> detections)
    {
        if (detections.Count == 0)
        {
            return "Suspicious activity";
        }

        if (detections.Count == 1)
        {
            return detections[0].Title;
        }

        var highest = detections.OrderByDescending(d => (int)d.Severity).First();
        return $"Multiple alerts: {highest.Title} (+{detections.Count - 1})";
    }

    public static IncidentType PreferType(IncidentType current, IncidentType incoming)
    {
        // Prefer more specific theft-oriented types when escalating.
        var rank = new Dictionary<IncidentType, int>
        {
            [IncidentType.SuspiciousActivity] = 0,
            [IncidentType.GpsAnomaly] = 1,
            [IncidentType.FuelAnomaly] = 2,
            [IncidentType.GeofenceViolation] = 3,
            [IncidentType.UnauthorizedUse] = 4,
            [IncidentType.PossibleTheft] = 5
        };

        return rank.GetValueOrDefault(incoming, 0) > rank.GetValueOrDefault(current, 0)
            ? incoming
            : current;
    }

    public static IncidentType InferTypeFromDetections(IReadOnlyList<Detection> detections)
    {
        if (detections.Count == 0)
        {
            return IncidentType.SuspiciousActivity;
        }

        var type = MapIncidentType(detections[0].DetectionType);
        foreach (var detection in detections.Skip(1))
        {
            type = PreferType(type, MapIncidentType(detection.DetectionType));
        }

        // Multiple distinct security signals → possible theft.
        var distinct = detections.Select(d => d.DetectionType).Distinct().Count();
        if (distinct >= 3)
        {
            type = PreferType(type, IncidentType.PossibleTheft);
        }

        return type;
    }
}
