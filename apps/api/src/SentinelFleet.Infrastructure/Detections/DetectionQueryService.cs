using Microsoft.EntityFrameworkCore;
using SentinelFleet.Application.Detections;
using SentinelFleet.Application.Security;
using SentinelFleet.Domain.Rules;
using SentinelFleet.Infrastructure.Persistence;

namespace SentinelFleet.Infrastructure.Detections;

public sealed class DetectionQueryService(
    SentinelFleetDbContext db,
    IOrganizationContext organizationContext) : IDetectionQueryService
{
    public async Task<DetectionResult<IReadOnlyList<DetectionDto>>> ListAsync(
        Guid? assetId = null,
        DetectionRuleType? detectionType = null,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        if (limit is < 1 or > 500)
        {
            return DetectionResult<IReadOnlyList<DetectionDto>>.Failure(
                new DetectionError(DetectionErrorCode.Validation, "Limit must be between 1 and 500."));
        }

        var query = db.Detections.AsNoTracking()
            .Where(d => d.OrganizationId == organizationContext.OrganizationId);

        if (assetId is Guid aid)
        {
            query = query.Where(d => d.AssetId == aid);
        }

        if (detectionType is DetectionRuleType type)
        {
            query = query.Where(d => d.DetectionType == type);
        }

        if (from is DateTimeOffset fromValue)
        {
            query = query.Where(d => d.TriggeredAt >= fromValue);
        }

        if (to is DateTimeOffset toValue)
        {
            query = query.Where(d => d.TriggeredAt <= toValue);
        }

        var items = await query
            .OrderByDescending(d => d.TriggeredAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return DetectionResult<IReadOnlyList<DetectionDto>>.Success(
            items.Select(d => new DetectionDto(
                d.Id,
                d.AssetId,
                d.RuleId,
                d.DetectionType,
                d.Severity,
                d.Confidence,
                d.RiskContribution,
                d.Title,
                d.Description,
                d.TriggeredAt,
                d.SourceEventIds,
                d.Metadata,
                d.IncidentId,
                d.CreatedAt)).ToList());
    }

    public async Task<DetectionResult<DetectionDto>> GetAsync(
        Guid detectionId,
        CancellationToken cancellationToken = default)
    {
        var detection = await db.Detections.AsNoTracking()
            .FirstOrDefaultAsync(
                d => d.Id == detectionId && d.OrganizationId == organizationContext.OrganizationId,
                cancellationToken);

        if (detection is null)
        {
            return DetectionResult<DetectionDto>.Failure(
                new DetectionError(DetectionErrorCode.NotFound, "Detection not found."));
        }

        return DetectionResult<DetectionDto>.Success(new DetectionDto(
            detection.Id,
            detection.AssetId,
            detection.RuleId,
            detection.DetectionType,
            detection.Severity,
            detection.Confidence,
            detection.RiskContribution,
            detection.Title,
            detection.Description,
            detection.TriggeredAt,
            detection.SourceEventIds,
            detection.Metadata,
            detection.IncidentId,
            detection.CreatedAt));
    }
}
