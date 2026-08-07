using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using SentinelFleet.Application.Rules;
using SentinelFleet.Application.Telemetry;
using SentinelFleet.Domain.Detections;
using SentinelFleet.Domain.Geofences;
using SentinelFleet.Domain.Rules;
using SentinelFleet.Domain.Telemetry;
using SentinelFleet.Infrastructure.Persistence;

namespace SentinelFleet.Infrastructure.Rules;

public sealed class RuleEngine(
    SentinelFleetDbContext db,
    IDetectionRuleService ruleService,
    IFleetRealtimePublisher realtimePublisher,
    ILogger<RuleEngine> logger) : IRuleEngine
{
    private static readonly GeometryFactory GeometryFactory =
        NetTopologySuite.NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

    public async Task<IReadOnlyList<Detection>> EvaluateTelemetryAsync(
        QueuedTelemetryMessage current,
        CancellationToken cancellationToken = default)
    {
        await ruleService.EnsureDefaultRulesAsync(current.OrganizationId, cancellationToken);

        var activeRules = await db.DetectionRules
            .Where(r => r.OrganizationId == current.OrganizationId && r.IsActive)
            .ToListAsync(cancellationToken);

        if (activeRules.Count == 0)
        {
            return [];
        }

        var previous = await db.TelemetryEvents.AsNoTracking()
            .Where(e => e.OrganizationId == current.OrganizationId &&
                        e.AssetId == current.AssetId &&
                        e.EventId != current.EventId)
            .OrderByDescending(e => e.RecordedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var created = new List<Detection>();

        var geofenceRules = activeRules
            .Where(r => r.RuleType is DetectionRuleType.GeofenceEnter or DetectionRuleType.GeofenceExit)
            .ToList();
        if (geofenceRules.Count > 0)
        {
            created.AddRange(await EvaluateGeofencesAsync(current, geofenceRules, cancellationToken));
        }

        foreach (var rule in activeRules)
        {
            Detection? detection = rule.RuleType switch
            {
                DetectionRuleType.OutsideWorkHours =>
                    await EvaluateOutsideWorkHoursAsync(current, rule, cancellationToken),
                DetectionRuleType.UnauthorizedUser =>
                    await EvaluateUnauthorizedAsync(current, rule, cancellationToken),
                DetectionRuleType.FuelLoss =>
                    await EvaluateFuelLossAsync(current, previous, rule, cancellationToken),
                _ => null
            };

            if (detection is not null)
            {
                created.Add(detection);
            }
        }

        if (created.Count > 0)
        {
            db.Detections.AddRange(created);
        }

        // Persist AssetPresence updates even when no detections were raised.
        if (db.ChangeTracker.HasChanges())
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        if (created.Count == 0)
        {
            return [];
        }

        foreach (var detection in created)
        {
            await realtimePublisher.PublishDetectionCreatedAsync(
                new DetectionCreatedMessage(
                    detection.OrganizationId,
                    detection.Id,
                    detection.AssetId,
                    detection.DetectionType.ToString(),
                    detection.Severity.ToString(),
                    detection.Title,
                    detection.TriggeredAt),
                cancellationToken);

            logger.LogInformation(
                "Detection {DetectionType} created for asset {AssetId}: {Title}",
                detection.DetectionType,
                detection.AssetId,
                detection.Title);
        }

        return created;
    }

    private async Task<List<Detection>> EvaluateGeofencesAsync(
        QueuedTelemetryMessage current,
        IReadOnlyList<DetectionRule> geofenceRules,
        CancellationToken cancellationToken)
    {
        var results = new List<Detection>();
        var now = current.RecordedAt;
        var point = GeometryFactory.CreatePoint(new Coordinate(current.Longitude, current.Latitude));

        var links = await db.AssetGeofences
            .Include(a => a.Geofence)
            .Where(a => a.OrganizationId == current.OrganizationId &&
                        a.AssetId == current.AssetId &&
                        a.Geofence.IsActive &&
                        (a.ValidFrom == null || a.ValidFrom <= now) &&
                        (a.ValidTo == null || a.ValidTo >= now))
            .ToListAsync(cancellationToken);

        foreach (var link in links)
        {
            var geofence = link.Geofence;
            var isInside = geofence.Geometry.Contains(point);

            var presence = await db.AssetPresences.FirstOrDefaultAsync(
                p => p.AssetId == current.AssetId && p.GeofenceId == geofence.Id,
                cancellationToken);

            bool? wasInside = presence?.IsInside;
            var transition = RuleEvaluation.GetTransition(wasInside, isInside);

            if (presence is null)
            {
                db.AssetPresences.Add(new AssetPresence
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = current.OrganizationId,
                    AssetId = current.AssetId,
                    GeofenceId = geofence.Id,
                    IsInside = isInside,
                    UpdatedAt = now
                });
            }
            else
            {
                presence.IsInside = isInside;
                presence.UpdatedAt = now;
            }

            if (transition == RuleEvaluation.GeofenceTransition.None)
            {
                continue;
            }

            var matchesLink =
                link.RuleType == AssetGeofenceRuleType.Both ||
                (transition == RuleEvaluation.GeofenceTransition.Enter &&
                 link.RuleType == AssetGeofenceRuleType.Enter) ||
                (transition == RuleEvaluation.GeofenceTransition.Exit &&
                 link.RuleType == AssetGeofenceRuleType.Exit);

            if (!matchesLink)
            {
                continue;
            }

            // Security signals: leave allowed zone, or enter restricted zone.
            DetectionRuleType? detectionType = null;
            if (transition == RuleEvaluation.GeofenceTransition.Exit &&
                geofence.GeofenceType == GeofenceType.Allowed)
            {
                detectionType = DetectionRuleType.GeofenceExit;
            }
            else if (transition == RuleEvaluation.GeofenceTransition.Enter &&
                     geofence.GeofenceType == GeofenceType.Restricted)
            {
                detectionType = DetectionRuleType.GeofenceEnter;
            }

            if (detectionType is null)
            {
                continue;
            }

            var rule = geofenceRules.FirstOrDefault(r => r.RuleType == detectionType);
            if (rule is null)
            {
                continue;
            }

            var config = RuleEvaluation.ParseConfig(rule.Configuration);
            if (await IsInCooldownAsync(
                    current.OrganizationId,
                    current.AssetId,
                    detectionType.Value,
                    config.CooldownMinutes,
                    now,
                    cancellationToken))
            {
                continue;
            }

            var verb = detectionType == DetectionRuleType.GeofenceExit ? "left" : "entered";
            results.Add(CreateDetection(
                current,
                rule,
                detectionType.Value,
                $"Asset {verb} geofence {geofence.Name}",
                $"Asset {verb} '{geofence.Name}' ({geofence.GeofenceType}).",
                new
                {
                    geofenceId = geofence.Id,
                    geofenceName = geofence.Name,
                    geofenceType = geofence.GeofenceType.ToString(),
                    transition = transition.ToString(),
                    latitude = current.Latitude,
                    longitude = current.Longitude
                }));
        }

        return results;
    }

    private async Task<Detection?> EvaluateOutsideWorkHoursAsync(
        QueuedTelemetryMessage current,
        DetectionRule rule,
        CancellationToken cancellationToken)
    {
        // Requires an identified driver. Avoids noisy alerts from anonymous fleet wander.
        if (current.DriverUserId is null)
        {
            return null;
        }

        if (!RuleEvaluation.IsMoving(current.SpeedKph, current.IgnitionOn))
        {
            return null;
        }

        var config = RuleEvaluation.ParseConfig(rule.Configuration);
        var outsideHours = RuleEvaluation.IsOutsideWorkHours(
            current.RecordedAt,
            config.WorkStartHourUtc,
            config.WorkEndHourUtc);

        var onShift = await db.WorkShifts.AnyAsync(
            s => s.OrganizationId == current.OrganizationId &&
                 s.UserId == current.DriverUserId &&
                 s.StartsAt <= current.RecordedAt &&
                 s.EndsAt >= current.RecordedAt &&
                 s.Status != "Cancelled",
            cancellationToken);

        // Allow: inside hours AND on shift.
        if (!outsideHours && onShift)
        {
            return null;
        }

        if (await IsInCooldownAsync(
                current.OrganizationId,
                current.AssetId,
                DetectionRuleType.OutsideWorkHours,
                config.CooldownMinutes,
                current.RecordedAt,
                cancellationToken))
        {
            return null;
        }

        var reason = outsideHours
            ? $"outside work hours (UTC {config.WorkStartHourUtc:00}-{config.WorkEndHourUtc:00})"
            : "without an active work shift";

        return CreateDetection(
            current,
            rule,
            DetectionRuleType.OutsideWorkHours,
            "Outside work hours",
            $"Driver used asset {reason}.",
            new
            {
                recordedAt = current.RecordedAt,
                workStartHourUtc = config.WorkStartHourUtc,
                workEndHourUtc = config.WorkEndHourUtc,
                outsideHours,
                onShift,
                driverUserId = current.DriverUserId,
                speedKph = current.SpeedKph,
                ignitionOn = current.IgnitionOn
            });
    }

    private async Task<Detection?> EvaluateUnauthorizedAsync(
        QueuedTelemetryMessage current,
        DetectionRule rule,
        CancellationToken cancellationToken)
    {
        if (current.DriverUserId is null)
        {
            return null;
        }

        var hasAssignment = await db.DriverAssignments.AnyAsync(
            a => a.OrganizationId == current.OrganizationId &&
                 a.AssetId == current.AssetId &&
                 a.UserId == current.DriverUserId &&
                 a.ValidFrom <= current.RecordedAt &&
                 (a.ValidTo == null || a.ValidTo >= current.RecordedAt),
            cancellationToken);

        if (!RuleEvaluation.IsUnauthorizedDriver(current.DriverUserId, hasAssignment))
        {
            return null;
        }

        var config = RuleEvaluation.ParseConfig(rule.Configuration);
        if (await IsInCooldownAsync(
                current.OrganizationId,
                current.AssetId,
                DetectionRuleType.UnauthorizedUser,
                config.CooldownMinutes,
                current.RecordedAt,
                cancellationToken))
        {
            return null;
        }

        return CreateDetection(
            current,
            rule,
            DetectionRuleType.UnauthorizedUser,
            "Unauthorized user",
            $"Driver {current.DriverUserId} is not assigned to this asset.",
            new { driverUserId = current.DriverUserId });
    }

    private async Task<Detection?> EvaluateFuelLossAsync(
        QueuedTelemetryMessage current,
        TelemetryEvent? previous,
        DetectionRule rule,
        CancellationToken cancellationToken)
    {
        if (previous is null)
        {
            return null;
        }

        var config = RuleEvaluation.ParseConfig(rule.Configuration);
        if (!RuleEvaluation.IsFuelLoss(
                previous.FuelLevelPercent,
                current.FuelLevelPercent,
                current.SpeedKph,
                current.IgnitionOn,
                config.DropPercent,
                config.MaxSpeedKph))
        {
            return null;
        }

        if (await IsInCooldownAsync(
                current.OrganizationId,
                current.AssetId,
                DetectionRuleType.FuelLoss,
                config.CooldownMinutes,
                current.RecordedAt,
                cancellationToken))
        {
            return null;
        }

        var drop = previous.FuelLevelPercent!.Value - current.FuelLevelPercent!.Value;
        return CreateDetection(
            current,
            rule,
            DetectionRuleType.FuelLoss,
            "Fuel loss detected",
            $"Fuel dropped {drop:0.0}% while stationary with ignition off.",
            new
            {
                previousFuelPercent = previous.FuelLevelPercent,
                currentFuelPercent = current.FuelLevelPercent,
                dropPercent = drop
            });
    }

    private async Task<bool> IsInCooldownAsync(
        Guid organizationId,
        Guid assetId,
        DetectionRuleType type,
        int cooldownMinutes,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var since = now.AddMinutes(-Math.Max(1, cooldownMinutes));
        return await db.Detections.AnyAsync(
            d => d.OrganizationId == organizationId &&
                 d.AssetId == assetId &&
                 d.DetectionType == type &&
                 d.TriggeredAt >= since,
            cancellationToken);
    }

    private static Detection CreateDetection(
        QueuedTelemetryMessage current,
        DetectionRule rule,
        DetectionRuleType type,
        string title,
        string description,
        object metadata) =>
        new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = current.OrganizationId,
            AssetId = current.AssetId,
            RuleId = rule.Id,
            DetectionType = type,
            Severity = rule.Severity,
            Confidence = 0.9,
            RiskContribution = RuleEvaluation.RiskContribution(rule.Severity),
            Title = title,
            Description = description,
            TriggeredAt = current.RecordedAt,
            SourceEventIds = JsonSerializer.Serialize(new[] { current.EventId }),
            Metadata = JsonSerializer.Serialize(metadata),
            IncidentId = null,
            CreatedAt = DateTimeOffset.UtcNow
        };
}
