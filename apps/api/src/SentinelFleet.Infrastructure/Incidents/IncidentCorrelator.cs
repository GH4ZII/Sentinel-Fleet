using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SentinelFleet.Application.Incidents;
using SentinelFleet.Application.Telemetry;
using SentinelFleet.Domain.Detections;
using SentinelFleet.Domain.Incidents;
using SentinelFleet.Infrastructure.Persistence;

namespace SentinelFleet.Infrastructure.Incidents;

public sealed class IncidentCorrelator(
    SentinelFleetDbContext db,
    IRiskScoringService riskScoring,
    IFleetRealtimePublisher realtimePublisher,
    ILogger<IncidentCorrelator> logger) : IIncidentCorrelator
{
    public async Task CorrelateAsync(
        IReadOnlyList<Detection> detections,
        CancellationToken cancellationToken = default)
    {
        if (detections.Count == 0)
        {
            return;
        }

        foreach (var group in detections.GroupBy(d => new { d.OrganizationId, d.AssetId }))
        {
            var list = group.OrderBy(d => d.TriggeredAt).ToList();
            var first = list[0];
            var createdNew = false;

            var openStatuses = new[] { IncidentStatus.Open, IncidentStatus.Investigating };
            var candidates = await db.Incidents
                .Where(i => i.OrganizationId == first.OrganizationId &&
                            i.PrimaryAssetId == first.AssetId &&
                            openStatuses.Contains(i.Status))
                .OrderByDescending(i => i.UpdatedAt)
                .ToListAsync(cancellationToken);

            var incident = candidates.FirstOrDefault(i =>
                IncidentCorrelation.IsWithinWindow(i.UpdatedAt, first.TriggeredAt) ||
                IncidentCorrelation.IsWithinWindow(i.DetectedAt, first.TriggeredAt));

            if (incident is null)
            {
                incident = new Incident
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = first.OrganizationId,
                    PrimaryAssetId = first.AssetId,
                    Title = IncidentCorrelation.BuildTitle(list),
                    Description = BuildDescription(list),
                    IncidentType = IncidentCorrelation.InferTypeFromDetections(list),
                    Status = IncidentStatus.Open,
                    Severity = IncidentCorrelation.MapSeverity(
                        list.MaxBy(d => (int)d.Severity)!.Severity),
                    RiskScore = 0,
                    Confidence = list.Average(d => d.Confidence),
                    StartedAt = list.Min(d => d.TriggeredAt),
                    DetectedAt = list.Max(d => d.TriggeredAt),
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                };
                db.Incidents.Add(incident);
                createdNew = true;

                db.IncidentTimelineEntries.Add(new IncidentTimelineEntry
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = incident.OrganizationId,
                    IncidentId = incident.Id,
                    EntryType = TimelineEntryType.System,
                    Timestamp = incident.CreatedAt,
                    Title = "Incident opened",
                    Description = "Automatic correlation created this incident from detections.",
                    SourceType = "System",
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
            else
            {
                var maxTriggered = list.Max(d => d.TriggeredAt);
                if (maxTriggered > incident.DetectedAt)
                {
                    incident.DetectedAt = maxTriggered;
                }

                if (list.Min(d => d.TriggeredAt) < incident.StartedAt)
                {
                    incident.StartedAt = list.Min(d => d.TriggeredAt);
                }

                incident.Severity = IncidentCorrelation.MaxSeverity(
                    incident.Severity,
                    IncidentCorrelation.MapSeverity(list.MaxBy(d => (int)d.Severity)!.Severity));
                incident.IncidentType = IncidentCorrelation.PreferType(
                    incident.IncidentType,
                    IncidentCorrelation.InferTypeFromDetections(list));
                incident.Title = IncidentCorrelation.BuildTitle(
                    await LoadAllDetectionsPreviewAsync(incident.Id, list, cancellationToken));
                incident.UpdatedAt = DateTimeOffset.UtcNow;
            }

            foreach (var detection in list)
            {
                // Attach tracked detection entity if available.
                var tracked = await db.Detections.FirstOrDefaultAsync(
                    d => d.Id == detection.Id,
                    cancellationToken);
                if (tracked is not null)
                {
                    tracked.IncidentId = incident.Id;
                }
                else
                {
                    detection.IncidentId = incident.Id;
                }

                var (lat, lon) = TryReadCoordinates(detection.Metadata);
                db.IncidentTimelineEntries.Add(new IncidentTimelineEntry
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = incident.OrganizationId,
                    IncidentId = incident.Id,
                    EntryType = TimelineEntryType.Detection,
                    Timestamp = detection.TriggeredAt,
                    Title = detection.Title,
                    Description = detection.Description,
                    SourceType = "Detection",
                    SourceId = detection.Id,
                    Latitude = lat,
                    Longitude = lon,
                    Metadata = detection.Metadata,
                    CreatedAt = DateTimeOffset.UtcNow
                });

                await UpsertEntityAsync(
                    incident,
                    "Asset",
                    detection.AssetId,
                    "involved",
                    detection.TriggeredAt,
                    cancellationToken);

                var geofenceId = TryReadGuid(detection.Metadata, "geofenceId");
                if (geofenceId is Guid gid)
                {
                    await UpsertEntityAsync(
                        incident,
                        "Geofence",
                        gid,
                        "related",
                        detection.TriggeredAt,
                        cancellationToken);
                }

                var driverId = TryReadGuid(detection.Metadata, "driverUserId");
                if (driverId is Guid did)
                {
                    await UpsertEntityAsync(
                        incident,
                        "User",
                        did,
                        "driver",
                        detection.TriggeredAt,
                        cancellationToken);
                }
            }

            await db.SaveChangesAsync(cancellationToken);
            await riskScoring.RecalculateAsync(incident.Id, cancellationToken);

            // Reload after risk update for accurate SignalR payload.
            await db.Entry(incident).ReloadAsync(cancellationToken);

            var message = new IncidentRealtimeMessage(
                incident.OrganizationId,
                incident.Id,
                incident.PrimaryAssetId,
                incident.Title,
                incident.Status.ToString(),
                incident.RiskScore,
                incident.Severity.ToString(),
                incident.UpdatedAt);

            if (createdNew)
            {
                await realtimePublisher.PublishIncidentCreatedAsync(message, cancellationToken);
            }
            else
            {
                await realtimePublisher.PublishIncidentUpdatedAsync(message, cancellationToken);
            }

            logger.LogInformation(
                "Correlated {Count} detection(s) into incident {IncidentId} (new={Created})",
                list.Count,
                incident.Id,
                createdNew);
        }
    }

    private async Task<List<Detection>> LoadAllDetectionsPreviewAsync(
        Guid incidentId,
        List<Detection> incoming,
        CancellationToken cancellationToken)
    {
        var existing = await db.Detections.AsNoTracking()
            .Where(d => d.IncidentId == incidentId)
            .ToListAsync(cancellationToken);
        return existing.Concat(incoming).GroupBy(d => d.Id).Select(g => g.First()).ToList();
    }

    private async Task UpsertEntityAsync(
        Incident incident,
        string entityType,
        Guid entityId,
        string relationshipType,
        DateTimeOffset observedAt,
        CancellationToken cancellationToken)
    {
        var existing = await db.IncidentEntities.FirstOrDefaultAsync(
            e => e.IncidentId == incident.Id &&
                 e.EntityType == entityType &&
                 e.EntityId == entityId,
            cancellationToken);

        if (existing is null)
        {
            db.IncidentEntities.Add(new IncidentEntity
            {
                Id = Guid.NewGuid(),
                OrganizationId = incident.OrganizationId,
                IncidentId = incident.Id,
                EntityType = entityType,
                EntityId = entityId,
                RelationshipType = relationshipType,
                FirstObservedAt = observedAt,
                LastObservedAt = observedAt
            });
            return;
        }

        if (observedAt < existing.FirstObservedAt)
        {
            existing.FirstObservedAt = observedAt;
        }

        if (observedAt > existing.LastObservedAt)
        {
            existing.LastObservedAt = observedAt;
        }
    }

    private static string BuildDescription(IReadOnlyList<Detection> detections) =>
        string.Join("; ", detections.Select(d => d.Title).Distinct().Take(5));

    private static (double? Lat, double? Lon) TryReadCoordinates(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return (null, null);
        }

        try
        {
            using var doc = JsonDocument.Parse(metadataJson);
            double? lat = null;
            double? lon = null;
            if (doc.RootElement.TryGetProperty("latitude", out var latEl) &&
                latEl.TryGetDouble(out var latVal))
            {
                lat = latVal;
            }

            if (doc.RootElement.TryGetProperty("longitude", out var lonEl) &&
                lonEl.TryGetDouble(out var lonVal))
            {
                lon = lonVal;
            }

            return (lat, lon);
        }
        catch (JsonException)
        {
            return (null, null);
        }
    }

    private static Guid? TryReadGuid(string? metadataJson, string property)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(metadataJson);
            if (doc.RootElement.TryGetProperty(property, out var el) &&
                el.ValueKind == JsonValueKind.String &&
                Guid.TryParse(el.GetString(), out var id))
            {
                return id;
            }
        }
        catch (JsonException)
        {
            // ignore
        }

        return null;
    }
}
