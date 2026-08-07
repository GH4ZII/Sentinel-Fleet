using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SentinelFleet.Application.Anomaly;
using SentinelFleet.Application.Incidents;
using SentinelFleet.Application.Telemetry;
using SentinelFleet.Domain.Anomaly;
using SentinelFleet.Domain.Detections;
using SentinelFleet.Domain.Rules;
using SentinelFleet.Infrastructure.Persistence;
using SentinelFleet.Infrastructure.Rules;

namespace SentinelFleet.Infrastructure.Anomaly;

public sealed class AnomalyEvaluator(
    SentinelFleetDbContext db,
    IAnomalyServiceClient anomalyClient,
    IIncidentCorrelator correlator,
    ILogger<AnomalyEvaluator> logger) : IAnomalyEvaluator
{
    private static readonly TimeSpan Cooldown = TimeSpan.FromMinutes(20);

    public async Task EvaluateTelemetryAsync(
        QueuedTelemetryMessage message,
        CancellationToken cancellationToken = default)
    {
        var recordedAt = message.RecordedAt.ToUniversalTime();
        var features = new AnomalyFeatureVector(
            HourOfDay: recordedAt.Hour + recordedAt.Minute / 60.0,
            DayOfWeek: (int)recordedAt.DayOfWeek,
            SpeedKph: message.SpeedKph ?? 0,
            IgnitionOn: message.IgnitionOn == true ? 1 : 0,
            FuelLevelPercent: message.FuelLevelPercent,
            OdometerKm: message.OdometerKm);

        var score = await anomalyClient.ScoreAsync(
            new AnomalyScoreRequest(
                message.OrganizationId,
                message.AssetId,
                message.EventId,
                message.RecordedAt,
                features),
            cancellationToken);

        if (score is null)
        {
            return;
        }

        var assessment = new AnomalyAssessment
        {
            Id = Guid.NewGuid(),
            OrganizationId = message.OrganizationId,
            AssetId = message.AssetId,
            TelemetryEventId = null,
            Score = score.AnomalyScore,
            Confidence = score.Confidence,
            ModelVersion = score.ModelVersion,
            Method = score.Method,
            FeaturesUsed = JsonSerializer.Serialize(score.FeaturesUsed),
            Explanation = score.Explanation,
            IsAnomaly = score.IsAnomaly,
            CalculatedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Link telemetry event id if present.
        var telemetry = await db.TelemetryEvents.AsNoTracking()
            .Where(e => e.EventId == message.EventId)
            .Select(e => new { e.Id })
            .FirstOrDefaultAsync(cancellationToken);
        if (telemetry is not null)
        {
            assessment.TelemetryEventId = telemetry.Id;
        }

        db.AnomalyAssessments.Add(assessment);

        if (!score.IsAnomaly)
        {
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        var recent = await db.Detections.AsNoTracking()
            .Where(d => d.OrganizationId == message.OrganizationId &&
                        d.AssetId == message.AssetId &&
                        d.DetectionType == DetectionRuleType.UsageAnomaly &&
                        d.TriggeredAt >= DateTimeOffset.UtcNow - Cooldown)
            .AnyAsync(cancellationToken);

        if (recent)
        {
            await db.SaveChangesAsync(cancellationToken);
            logger.LogDebug(
                "Skipping usage anomaly detection for asset {AssetId} due to cooldown",
                message.AssetId);
            return;
        }

        var severity = score.AnomalyScore switch
        {
            >= 0.85 => DetectionSeverity.Critical,
            >= 0.7 => DetectionSeverity.High,
            >= 0.55 => DetectionSeverity.Medium,
            _ => DetectionSeverity.Low
        };

        var detection = new Detection
        {
            Id = Guid.NewGuid(),
            OrganizationId = message.OrganizationId,
            AssetId = message.AssetId,
            DetectionType = DetectionRuleType.UsageAnomaly,
            Severity = severity,
            Confidence = score.Confidence,
            RiskContribution = RuleEvaluation.RiskContribution(severity),
            Title = "Usage anomaly detected",
            Description = score.Explanation,
            TriggeredAt = message.RecordedAt,
            SourceEventIds = JsonSerializer.Serialize(new[] { message.EventId }),
            Metadata = JsonSerializer.Serialize(new
            {
                anomalyScore = score.AnomalyScore,
                modelVersion = score.ModelVersion,
                method = score.Method,
                featuresUsed = score.FeaturesUsed,
                latitude = message.Latitude,
                longitude = message.Longitude,
                assessmentId = assessment.Id
            }),
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.Detections.Add(detection);
        await db.SaveChangesAsync(cancellationToken);

        assessment.IncidentId = null;
        await correlator.CorrelateAsync([detection], cancellationToken);

        // Refresh assessment with incident id if correlated.
        var linked = await db.Detections.AsNoTracking()
            .Where(d => d.Id == detection.Id)
            .Select(d => d.IncidentId)
            .FirstOrDefaultAsync(cancellationToken);
        if (linked is Guid incidentId)
        {
            var tracked = await db.AnomalyAssessments.FirstOrDefaultAsync(
                a => a.Id == assessment.Id,
                cancellationToken);
            if (tracked is not null)
            {
                tracked.IncidentId = incidentId;
                await db.SaveChangesAsync(cancellationToken);
            }
        }

        logger.LogInformation(
            "Usage anomaly detection {DetectionId} for asset {AssetId} score={Score:F2} method={Method}",
            detection.Id,
            message.AssetId,
            score.AnomalyScore,
            score.Method);
    }
}
