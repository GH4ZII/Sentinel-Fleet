using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SentinelFleet.Application.Incidents;
using SentinelFleet.Application.Rules;
using SentinelFleet.Application.Telemetry;
using SentinelFleet.Domain.Detections;
using SentinelFleet.Domain.Rules;
using SentinelFleet.Infrastructure.Persistence;

namespace SentinelFleet.Infrastructure.Rules;

public sealed class GpsOfflineWatcher(
    IServiceScopeFactory scopeFactory,
    ILogger<GpsOfflineWatcher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var evaluator = scope.ServiceProvider.GetRequiredService<IGpsOfflineEvaluator>();
                await evaluator.EvaluateAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GPS offline watcher failed");
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}

public sealed class GpsOfflineEvaluator(
    SentinelFleetDbContext db,
    IDetectionRuleService ruleService,
    IIncidentCorrelator incidentCorrelator,
    IFleetRealtimePublisher realtimePublisher,
    ILogger<GpsOfflineEvaluator> logger) : IGpsOfflineEvaluator
{
    public async Task EvaluateAsync(CancellationToken cancellationToken = default)
    {
        var orgIds = await db.DetectionRules
            .Where(r => r.IsActive && r.RuleType == DetectionRuleType.GpsOffline)
            .Select(r => r.OrganizationId)
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (var orgId in orgIds)
        {
            await ruleService.EnsureDefaultRulesAsync(orgId, cancellationToken);

            var rule = await db.DetectionRules.FirstOrDefaultAsync(
                r => r.OrganizationId == orgId &&
                     r.RuleType == DetectionRuleType.GpsOffline &&
                     r.IsActive,
                cancellationToken);
            if (rule is null)
            {
                continue;
            }

            var config = RuleEvaluation.ParseConfig(rule.Configuration);
            var offlineSince = DateTimeOffset.UtcNow.AddMinutes(-Math.Max(1, config.OfflineMinutes));
            var recentlyMovingSince = DateTimeOffset.UtcNow.AddHours(-6);

            var candidates = await db.Devices
                .Where(d => d.OrganizationId == orgId &&
                            d.AssetId != null &&
                            d.LastSeenAt != null &&
                            d.LastSeenAt < offlineSince &&
                            d.LastSeenAt > recentlyMovingSince)
                .Select(d => new { AssetId = d.AssetId!.Value, d.LastSeenAt })
                .ToListAsync(cancellationToken);

            foreach (var candidate in candidates)
            {
                var cooldownSince = DateTimeOffset.UtcNow.AddMinutes(-Math.Max(1, config.CooldownMinutes));
                var already = await db.Detections.AnyAsync(
                    d => d.OrganizationId == orgId &&
                         d.AssetId == candidate.AssetId &&
                         d.DetectionType == DetectionRuleType.GpsOffline &&
                         d.TriggeredAt >= cooldownSince,
                    cancellationToken);
                if (already)
                {
                    continue;
                }

                var recentMotion = await db.TelemetryEvents.AsNoTracking()
                    .Where(e => e.OrganizationId == orgId &&
                                e.AssetId == candidate.AssetId &&
                                e.RecordedAt >= recentlyMovingSince)
                    .OrderByDescending(e => e.RecordedAt)
                    .Select(e => new { e.SpeedKph, e.IgnitionOn, e.EventId })
                    .FirstOrDefaultAsync(cancellationToken);

                if (recentMotion is null)
                {
                    continue;
                }

                // Alert when the asset was recently active (moving or ignition on).
                if (!RuleEvaluation.IsMoving(recentMotion.SpeedKph, recentMotion.IgnitionOn))
                {
                    continue;
                }

                var triggeredAt = candidate.LastSeenAt!.Value;
                var detection = new Detection
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = orgId,
                    AssetId = candidate.AssetId,
                    RuleId = rule.Id,
                    DetectionType = DetectionRuleType.GpsOffline,
                    Severity = rule.Severity,
                    Confidence = 0.8,
                    RiskContribution = RuleEvaluation.RiskContribution(rule.Severity),
                    Title = "GPS offline",
                    Description =
                        $"No telemetry received for more than {config.OfflineMinutes} minutes.",
                    TriggeredAt = triggeredAt,
                    SourceEventIds = JsonSerializer.Serialize(new[] { recentMotion.EventId }),
                    Metadata = JsonSerializer.Serialize(new
                    {
                        lastSeenAt = candidate.LastSeenAt,
                        offlineMinutes = config.OfflineMinutes
                    }),
                    IncidentId = null,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                db.Detections.Add(detection);
                await db.SaveChangesAsync(cancellationToken);

                await incidentCorrelator.CorrelateAsync([detection], cancellationToken);

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
                    "GPS offline detection for asset {AssetId} in org {OrganizationId}",
                    detection.AssetId,
                    orgId);
            }
        }
    }
}
