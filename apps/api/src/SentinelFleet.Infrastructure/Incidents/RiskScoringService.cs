using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SentinelFleet.Application.Incidents;
using SentinelFleet.Domain.Incidents;
using SentinelFleet.Infrastructure.Persistence;

namespace SentinelFleet.Infrastructure.Incidents;

public sealed class RiskScoringService(
    SentinelFleetDbContext db,
    ILogger<RiskScoringService> logger) : IRiskScoringService
{
    public async Task<RiskAssessment> RecalculateAsync(
        Guid incidentId,
        CancellationToken cancellationToken = default)
    {
        var incident = await db.Incidents.FirstOrDefaultAsync(
            i => i.Id == incidentId,
            cancellationToken)
            ?? throw new InvalidOperationException($"Incident {incidentId} not found.");

        var detections = await db.Detections
            .Where(d => d.IncidentId == incidentId)
            .OrderBy(d => d.TriggeredAt)
            .ToListAsync(cancellationToken);

        var asset = await db.Assets.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == incident.PrimaryAssetId, cancellationToken);

        var result = RiskScoring.Calculate(detections, asset?.Criticality);
        var previousScore = incident.RiskScore;

        var assessment = new RiskAssessment
        {
            Id = Guid.NewGuid(),
            OrganizationId = incident.OrganizationId,
            IncidentId = incident.Id,
            Score = result.Score,
            RiskLevel = result.RiskLevel,
            Factors = RiskScoring.SerializeFactors(result.Factors),
            ModelVersion = RiskScoring.ModelVersion,
            CalculatedAt = DateTimeOffset.UtcNow
        };

        db.RiskAssessments.Add(assessment);
        incident.RiskScore = result.Score;
        incident.Severity = result.Severity;
        incident.UpdatedAt = DateTimeOffset.UtcNow;

        if (previousScore != result.Score)
        {
            db.IncidentTimelineEntries.Add(new IncidentTimelineEntry
            {
                Id = Guid.NewGuid(),
                OrganizationId = incident.OrganizationId,
                IncidentId = incident.Id,
                EntryType = TimelineEntryType.Risk,
                Timestamp = assessment.CalculatedAt,
                Title = $"Risk score {result.Score}",
                Description =
                    $"Risk level {result.RiskLevel}. Factors: {string.Join(", ", result.Factors.Select(f => $"{f.Label} (+{f.Points})"))}",
                SourceType = "RiskAssessment",
                SourceId = assessment.Id,
                Metadata = assessment.Factors,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Risk recalculated for incident {IncidentId}: {Score} ({Level})",
            incidentId,
            result.Score,
            result.RiskLevel);

        return assessment;
    }
}
