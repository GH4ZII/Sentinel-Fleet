using Microsoft.EntityFrameworkCore;
using SentinelFleet.Application.Rules;
using SentinelFleet.Application.Security;
using SentinelFleet.Domain.Rules;
using SentinelFleet.Infrastructure.Persistence;

namespace SentinelFleet.Infrastructure.Rules;

public sealed class DetectionRuleService(
    SentinelFleetDbContext db,
    IOrganizationContext organizationContext) : IDetectionRuleService
{
    public async Task<RuleResult<IReadOnlyList<DetectionRuleDto>>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        await EnsureDefaultRulesAsync(organizationContext.OrganizationId, cancellationToken);

        var rules = await db.DetectionRules.AsNoTracking()
            .Where(r => r.OrganizationId == organizationContext.OrganizationId)
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);

        return RuleResult<IReadOnlyList<DetectionRuleDto>>.Success(rules.Select(ToDto).ToList());
    }

    public async Task<RuleResult<DetectionRuleDto>> GetAsync(
        Guid ruleId,
        CancellationToken cancellationToken = default)
    {
        var rule = await FindInOrgAsync(ruleId, cancellationToken);
        if (rule is null)
        {
            return RuleResult<DetectionRuleDto>.Failure(
                new RuleError(RuleErrorCode.NotFound, "Rule not found."));
        }

        return RuleResult<DetectionRuleDto>.Success(ToDto(rule));
    }

    public async Task<RuleResult<DetectionRuleDto>> CreateAsync(
        CreateDetectionRuleRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!organizationContext.CanMutate)
        {
            return RuleResult<DetectionRuleDto>.Failure(
                new RuleError(RuleErrorCode.Forbidden, "Insufficient permissions."));
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return RuleResult<DetectionRuleDto>.Failure(
                new RuleError(RuleErrorCode.Validation, "Name is required."));
        }

        var now = DateTimeOffset.UtcNow;
        var rule = new DetectionRule
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationContext.OrganizationId,
            Name = request.Name.Trim(),
            RuleType = request.RuleType,
            Description = request.Description,
            Configuration = request.Configuration,
            Severity = request.Severity,
            IsActive = request.IsActive,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.DetectionRules.Add(rule);
        await db.SaveChangesAsync(cancellationToken);
        return RuleResult<DetectionRuleDto>.Success(ToDto(rule));
    }

    public async Task<RuleResult<DetectionRuleDto>> UpdateAsync(
        Guid ruleId,
        UpdateDetectionRuleRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!organizationContext.CanMutate)
        {
            return RuleResult<DetectionRuleDto>.Failure(
                new RuleError(RuleErrorCode.Forbidden, "Insufficient permissions."));
        }

        var rule = await FindInOrgAsync(ruleId, cancellationToken);
        if (rule is null)
        {
            return RuleResult<DetectionRuleDto>.Failure(
                new RuleError(RuleErrorCode.NotFound, "Rule not found."));
        }

        if (request.Name is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return RuleResult<DetectionRuleDto>.Failure(
                    new RuleError(RuleErrorCode.Validation, "Name is required."));
            }

            rule.Name = request.Name.Trim();
        }

        if (request.Description is not null)
        {
            rule.Description = request.Description;
        }

        if (request.Configuration is not null)
        {
            rule.Configuration = request.Configuration;
        }

        if (request.Severity is DetectionSeverity severity)
        {
            rule.Severity = severity;
        }

        if (request.IsActive is bool active)
        {
            rule.IsActive = active;
        }

        rule.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return RuleResult<DetectionRuleDto>.Success(ToDto(rule));
    }

    public Task<RuleResult<DetectionRuleDto>> EnableAsync(
        Guid ruleId,
        CancellationToken cancellationToken = default) =>
        SetActiveAsync(ruleId, true, cancellationToken);

    public Task<RuleResult<DetectionRuleDto>> DisableAsync(
        Guid ruleId,
        CancellationToken cancellationToken = default) =>
        SetActiveAsync(ruleId, false, cancellationToken);

    public async Task EnsureDefaultRulesAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        var existingTypes = await db.DetectionRules
            .Where(r => r.OrganizationId == organizationId)
            .Select(r => r.RuleType)
            .ToListAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        foreach (var definition in DefaultRules)
        {
            if (existingTypes.Contains(definition.Type))
            {
                continue;
            }

            db.DetectionRules.Add(new DetectionRule
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                Name = definition.Name,
                RuleType = definition.Type,
                Description = definition.Description,
                Configuration = definition.Configuration,
                Severity = definition.Severity,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        if (db.ChangeTracker.HasChanges())
        {
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<RuleResult<DetectionRuleDto>> SetActiveAsync(
        Guid ruleId,
        bool isActive,
        CancellationToken cancellationToken)
    {
        if (!organizationContext.CanMutate)
        {
            return RuleResult<DetectionRuleDto>.Failure(
                new RuleError(RuleErrorCode.Forbidden, "Insufficient permissions."));
        }

        var rule = await FindInOrgAsync(ruleId, cancellationToken);
        if (rule is null)
        {
            return RuleResult<DetectionRuleDto>.Failure(
                new RuleError(RuleErrorCode.NotFound, "Rule not found."));
        }

        rule.IsActive = isActive;
        rule.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return RuleResult<DetectionRuleDto>.Success(ToDto(rule));
    }

    private async Task<DetectionRule?> FindInOrgAsync(Guid ruleId, CancellationToken cancellationToken)
    {
        return await db.DetectionRules.FirstOrDefaultAsync(
            r => r.Id == ruleId && r.OrganizationId == organizationContext.OrganizationId,
            cancellationToken);
    }

    private static DetectionRuleDto ToDto(DetectionRule rule) =>
        new(
            rule.Id,
            rule.Name,
            rule.RuleType,
            rule.Description,
            rule.Configuration,
            rule.Severity,
            rule.IsActive,
            rule.CreatedAt,
            rule.UpdatedAt);

    private static readonly (DetectionRuleType Type, string Name, string Description, string Configuration, DetectionSeverity Severity)[] DefaultRules =
    [
        (DetectionRuleType.GeofenceExit, "Geofence exit", "Detect when an asset leaves an allowed geofence.",
            """{"cooldownMinutes":5}""", DetectionSeverity.High),
        (DetectionRuleType.GeofenceEnter, "Geofence enter", "Detect when an asset enters a restricted geofence.",
            """{"cooldownMinutes":5}""", DetectionSeverity.High),
        (DetectionRuleType.OutsideWorkHours, "Outside work hours", "Detect movement outside configured work hours.",
            """{"workStartHourUtc":7,"workEndHourUtc":17,"cooldownMinutes":15}""", DetectionSeverity.Medium),
        (DetectionRuleType.GpsOffline, "GPS offline", "Detect when GPS telemetry stops unexpectedly.",
            """{"offlineMinutes":5,"cooldownMinutes":30}""", DetectionSeverity.High),
        (DetectionRuleType.UnauthorizedUser, "Unauthorized user", "Detect driver without an active assignment.",
            """{"cooldownMinutes":15}""", DetectionSeverity.High),
        (DetectionRuleType.FuelLoss, "Fuel loss", "Detect sudden fuel drop while stationary with ignition off.",
            """{"dropPercent":8,"maxSpeedKph":1,"cooldownMinutes":30}""", DetectionSeverity.Critical)
    ];
}
