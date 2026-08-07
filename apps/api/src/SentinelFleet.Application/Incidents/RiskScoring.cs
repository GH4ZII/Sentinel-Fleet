using System.Text.Json;
using SentinelFleet.Domain.Assets;
using SentinelFleet.Domain.Detections;
using SentinelFleet.Domain.Incidents;
using SentinelFleet.Domain.Rules;

namespace SentinelFleet.Application.Incidents;

public sealed record RiskFactor(string Code, string Label, int Points, string Explanation);

public sealed record RiskScoreResult(
    int Score,
    RiskLevel RiskLevel,
    IncidentSeverity Severity,
    IReadOnlyList<RiskFactor> Factors);

public static class RiskScoring
{
    public const string ModelVersion = "v1-sum-capped";

    public static RiskLevel ToRiskLevel(int score) =>
        score switch
        {
            >= 80 => RiskLevel.Critical,
            >= 60 => RiskLevel.High,
            >= 30 => RiskLevel.Moderate,
            _ => RiskLevel.Low
        };

    public static IncidentSeverity ToSeverity(int score) =>
        score switch
        {
            >= 80 => IncidentSeverity.Critical,
            >= 60 => IncidentSeverity.High,
            >= 30 => IncidentSeverity.Medium,
            _ => IncidentSeverity.Low
        };

    public static RiskScoreResult Calculate(
        IReadOnlyList<Detection> detections,
        AssetCriticality? assetCriticality = null)
    {
        var factors = new List<RiskFactor>();
        var seenTypes = new HashSet<DetectionRuleType>();

        foreach (var detection in detections.OrderBy(d => d.TriggeredAt))
        {
            var code = detection.DetectionType.ToString();
            if (!seenTypes.Add(detection.DetectionType))
            {
                // Subsequent same-type detections contribute half (rounded down).
                var half = Math.Max(1, detection.RiskContribution / 2);
                factors.Add(new RiskFactor(
                    code,
                    detection.Title,
                    half,
                    $"Repeat {detection.DetectionType} alert (half contribution)."));
                continue;
            }

            factors.Add(new RiskFactor(
                code,
                detection.Title,
                detection.RiskContribution,
                detection.Description ?? $"{detection.DetectionType} contributed {detection.RiskContribution}."));
        }

        if (assetCriticality is AssetCriticality.High or AssetCriticality.Critical)
        {
            var points = assetCriticality == AssetCriticality.Critical ? 15 : 10;
            factors.Add(new RiskFactor(
                "AssetCriticality",
                "High-value asset",
                points,
                $"Asset criticality is {assetCriticality}."));
        }

        if (detections.Any(d => d.DetectionType is DetectionRuleType.GeofenceEnter or DetectionRuleType.GeofenceExit) &&
            detections.Any(d => d.DetectionType == DetectionRuleType.GpsOffline))
        {
            factors.Add(new RiskFactor(
                "CompoundSignal",
                "Geofence + GPS loss",
                10,
                "Geofence breach combined with GPS loss elevates suspicion."));
        }

        var score = Math.Clamp(factors.Sum(f => f.Points), 0, 100);
        return new RiskScoreResult(score, ToRiskLevel(score), ToSeverity(score), factors);
    }

    public static string SerializeFactors(IReadOnlyList<RiskFactor> factors) =>
        JsonSerializer.Serialize(factors.Select(f => new
        {
            code = f.Code,
            label = f.Label,
            points = f.Points,
            explanation = f.Explanation
        }));
}
