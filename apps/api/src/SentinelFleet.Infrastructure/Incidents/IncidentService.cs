using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SentinelFleet.Application.Incidents;
using SentinelFleet.Application.Security;
using SentinelFleet.Application.Telemetry;
using SentinelFleet.Domain.Audit;
using SentinelFleet.Domain.Incidents;
using SentinelFleet.Infrastructure.Persistence;

namespace SentinelFleet.Infrastructure.Incidents;

public sealed class IncidentService(
    SentinelFleetDbContext db,
    IOrganizationContext organizationContext,
    IAttachmentStorage attachmentStorage,
    IFleetRealtimePublisher realtimePublisher) : IIncidentService
{
    private static readonly TimeSpan PositionBuffer = TimeSpan.FromMinutes(15);

    public async Task<IncidentResult<IReadOnlyList<IncidentDto>>> ListAsync(
        Guid? assetId = null,
        IncidentStatus? status = null,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        if (limit is < 1 or > 500)
        {
            return FailList("Limit must be between 1 and 500.");
        }

        var query = db.Incidents.AsNoTracking()
            .Where(i => i.OrganizationId == organizationContext.OrganizationId);

        if (assetId is Guid aid)
        {
            query = query.Where(i => i.PrimaryAssetId == aid);
        }

        if (status is IncidentStatus st)
        {
            query = query.Where(i => i.Status == st);
        }

        var incidents = await query
            .OrderByDescending(i => i.DetectedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

        var ids = incidents.Select(i => i.Id).ToList();
        var detectionCounts = await db.Detections.AsNoTracking()
            .Where(d => d.IncidentId != null && ids.Contains(d.IncidentId.Value))
            .GroupBy(d => d.IncidentId!.Value)
            .Select(g => new { IncidentId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.IncidentId, x => x.Count, cancellationToken);

        var latestRisk = await LoadLatestRiskAsync(ids, cancellationToken);

        var dtos = incidents.Select(i => MapIncident(
            i,
            detectionCounts.GetValueOrDefault(i.Id),
            latestRisk.GetValueOrDefault(i.Id))).ToList();

        return IncidentResult<IReadOnlyList<IncidentDto>>.Success(dtos);
    }

    public async Task<IncidentResult<IncidentDetailDto>> GetAsync(
        Guid incidentId,
        CancellationToken cancellationToken = default)
    {
        var incident = await FindIncidentAsync(incidentId, tracking: false, cancellationToken);
        if (incident is null)
        {
            return NotFoundDetail();
        }

        var detections = await db.Detections.AsNoTracking()
            .Where(d => d.IncidentId == incidentId)
            .OrderBy(d => d.TriggeredAt)
            .ToListAsync(cancellationToken);

        var timeline = await db.IncidentTimelineEntries.AsNoTracking()
            .Where(e => e.IncidentId == incidentId)
            .OrderBy(e => e.Timestamp)
            .ToListAsync(cancellationToken);

        var relationships = await db.IncidentEntities.AsNoTracking()
            .Where(e => e.IncidentId == incidentId)
            .OrderBy(e => e.FirstObservedAt)
            .ToListAsync(cancellationToken);

        var comments = await db.IncidentComments.AsNoTracking()
            .Where(c => c.IncidentId == incidentId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        var attachments = await db.IncidentAttachments.AsNoTracking()
            .Where(a => a.IncidentId == incidentId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);

        var audit = await db.AuditLogs.AsNoTracking()
            .Where(a => a.EntityType == "Incident" && a.EntityId == incidentId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);

        var latestRisk = await db.RiskAssessments.AsNoTracking()
            .Where(r => r.IncidentId == incidentId)
            .OrderByDescending(r => r.CalculatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var dto = new IncidentDetailDto(
            MapIncident(incident, detections.Count, latestRisk is null ? null : MapRisk(latestRisk)),
            detections.Select(d => new DetectionSummaryDto(
                d.Id,
                d.DetectionType.ToString(),
                d.Severity.ToString(),
                d.RiskContribution,
                d.Title,
                d.Description,
                d.TriggeredAt,
                d.Metadata)).ToList(),
            timeline.Select(MapTimeline).ToList(),
            relationships.Select(MapEntity).ToList(),
            comments.Select(MapComment).ToList(),
            attachments.Select(MapAttachment).ToList(),
            audit.Select(MapAudit).ToList());

        return IncidentResult<IncidentDetailDto>.Success(dto);
    }

    public async Task<IncidentResult<IncidentDto>> UpdateAsync(
        Guid incidentId,
        UpdateIncidentRequest request,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        var incident = await FindIncidentAsync(incidentId, tracking: true, cancellationToken);
        if (incident is null)
        {
            return NotFound();
        }

        var oldValues = Snapshot(incident);
        if (request.Title is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return Fail("Title cannot be empty.");
            }

            incident.Title = request.Title.Trim();
        }

        if (request.Description is not null)
        {
            incident.Description = request.Description.Trim();
        }

        if (request.Status is IncidentStatus status)
        {
            incident.Status = status;
            if (status is IncidentStatus.Resolved or IncidentStatus.Dismissed)
            {
                incident.EndedAt ??= DateTimeOffset.UtcNow;
            }
        }

        incident.UpdatedAt = DateTimeOffset.UtcNow;
        await WriteAuditAsync(incident, "IncidentUpdated", oldValues, Snapshot(incident), ipAddress, cancellationToken);
        AddSystemTimeline(incident, "Incident updated", $"Status={incident.Status}");
        await db.SaveChangesAsync(cancellationToken);
        await PublishUpdatedAsync(incident, cancellationToken);

        return IncidentResult<IncidentDto>.Success(await ToDtoAsync(incident, cancellationToken));
    }

    public async Task<IncidentResult<IReadOnlyList<IncidentTimelineEntryDto>>> GetTimelineAsync(
        Guid incidentId,
        TimelineEntryType? entryType = null,
        CancellationToken cancellationToken = default)
    {
        if (await FindIncidentAsync(incidentId, tracking: false, cancellationToken) is null)
        {
            return IncidentResult<IReadOnlyList<IncidentTimelineEntryDto>>.Failure(
                new IncidentError(IncidentErrorCode.NotFound, "Incident not found."));
        }

        var query = db.IncidentTimelineEntries.AsNoTracking()
            .Where(e => e.IncidentId == incidentId &&
                        e.OrganizationId == organizationContext.OrganizationId);

        if (entryType is TimelineEntryType type)
        {
            query = query.Where(e => e.EntryType == type);
        }

        var entries = await query.OrderBy(e => e.Timestamp).ToListAsync(cancellationToken);
        return IncidentResult<IReadOnlyList<IncidentTimelineEntryDto>>.Success(
            entries.Select(MapTimeline).ToList());
    }

    public async Task<IncidentResult<IReadOnlyList<IncidentEntityDto>>> GetRelationshipsAsync(
        Guid incidentId,
        CancellationToken cancellationToken = default)
    {
        if (await FindIncidentAsync(incidentId, tracking: false, cancellationToken) is null)
        {
            return IncidentResult<IReadOnlyList<IncidentEntityDto>>.Failure(
                new IncidentError(IncidentErrorCode.NotFound, "Incident not found."));
        }

        var entities = await db.IncidentEntities.AsNoTracking()
            .Where(e => e.IncidentId == incidentId)
            .OrderBy(e => e.FirstObservedAt)
            .ToListAsync(cancellationToken);

        return IncidentResult<IReadOnlyList<IncidentEntityDto>>.Success(
            entities.Select(MapEntity).ToList());
    }

    public async Task<IncidentResult<IReadOnlyList<IncidentPositionDto>>> GetPositionsAsync(
        Guid incidentId,
        CancellationToken cancellationToken = default)
    {
        var incident = await FindIncidentAsync(incidentId, tracking: false, cancellationToken);
        if (incident is null)
        {
            return IncidentResult<IReadOnlyList<IncidentPositionDto>>.Failure(
                new IncidentError(IncidentErrorCode.NotFound, "Incident not found."));
        }

        var from = incident.StartedAt - PositionBuffer;
        var to = (incident.EndedAt ?? DateTimeOffset.UtcNow) + PositionBuffer;

        var positions = await db.TelemetryEvents.AsNoTracking()
            .Where(e => e.OrganizationId == incident.OrganizationId &&
                        e.AssetId == incident.PrimaryAssetId &&
                        e.RecordedAt >= from &&
                        e.RecordedAt <= to)
            .OrderBy(e => e.RecordedAt)
            .Take(2000)
            .Select(e => new IncidentPositionDto(
                e.EventId,
                e.Latitude,
                e.Longitude,
                e.SpeedKph,
                e.Heading,
                e.RecordedAt))
            .ToListAsync(cancellationToken);

        return IncidentResult<IReadOnlyList<IncidentPositionDto>>.Success(positions);
    }

    public async Task<IncidentResult<IncidentCommentDto>> AddCommentAsync(
        Guid incidentId,
        AddIncidentCommentRequest request,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return IncidentResult<IncidentCommentDto>.Failure(
                new IncidentError(IncidentErrorCode.Validation, "Comment content is required."));
        }

        var incident = await FindIncidentAsync(incidentId, tracking: true, cancellationToken);
        if (incident is null)
        {
            return IncidentResult<IncidentCommentDto>.Failure(
                new IncidentError(IncidentErrorCode.NotFound, "Incident not found."));
        }

        var now = DateTimeOffset.UtcNow;
        var comment = new IncidentComment
        {
            Id = Guid.NewGuid(),
            OrganizationId = incident.OrganizationId,
            IncidentId = incident.Id,
            UserId = organizationContext.UserId,
            Content = request.Content.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };

        db.IncidentComments.Add(comment);
        db.IncidentTimelineEntries.Add(new IncidentTimelineEntry
        {
            Id = Guid.NewGuid(),
            OrganizationId = incident.OrganizationId,
            IncidentId = incident.Id,
            EntryType = TimelineEntryType.Comment,
            Timestamp = now,
            Title = "Comment added",
            Description = comment.Content,
            SourceType = "Comment",
            SourceId = comment.Id,
            CreatedByUserId = organizationContext.UserId,
            CreatedAt = now
        });

        incident.UpdatedAt = now;
        await WriteAuditAsync(
            incident,
            "CommentAdded",
            null,
            JsonSerializer.Serialize(new { comment.Id, comment.Content }),
            ipAddress,
            cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await PublishUpdatedAsync(incident, cancellationToken);

        return IncidentResult<IncidentCommentDto>.Success(MapComment(comment));
    }

    public async Task<IncidentResult<IncidentAttachmentDto>> AddAttachmentAsync(
        Guid incidentId,
        string fileName,
        string contentType,
        Stream content,
        long size,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return IncidentResult<IncidentAttachmentDto>.Failure(
                new IncidentError(IncidentErrorCode.Validation, "File name is required."));
        }

        if (size <= 0 || size > 20 * 1024 * 1024)
        {
            return IncidentResult<IncidentAttachmentDto>.Failure(
                new IncidentError(IncidentErrorCode.Validation, "File size must be between 1 byte and 20 MB."));
        }

        var incident = await FindIncidentAsync(incidentId, tracking: true, cancellationToken);
        if (incident is null)
        {
            return IncidentResult<IncidentAttachmentDto>.Failure(
                new IncidentError(IncidentErrorCode.NotFound, "Incident not found."));
        }

        var storageKey = await attachmentStorage.SaveAsync(
            incident.OrganizationId,
            incident.Id,
            fileName,
            content,
            cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var attachment = new IncidentAttachment
        {
            Id = Guid.NewGuid(),
            OrganizationId = incident.OrganizationId,
            IncidentId = incident.Id,
            UploadedByUserId = organizationContext.UserId,
            Name = Path.GetFileName(fileName),
            ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            StorageKey = storageKey,
            Size = size,
            CreatedAt = now
        };

        db.IncidentAttachments.Add(attachment);
        db.IncidentTimelineEntries.Add(new IncidentTimelineEntry
        {
            Id = Guid.NewGuid(),
            OrganizationId = incident.OrganizationId,
            IncidentId = incident.Id,
            EntryType = TimelineEntryType.Attachment,
            Timestamp = now,
            Title = "Attachment uploaded",
            Description = attachment.Name,
            SourceType = "Attachment",
            SourceId = attachment.Id,
            CreatedByUserId = organizationContext.UserId,
            CreatedAt = now
        });

        incident.UpdatedAt = now;
        await WriteAuditAsync(
            incident,
            "AttachmentAdded",
            null,
            JsonSerializer.Serialize(new { attachment.Id, attachment.Name, attachment.Size }),
            ipAddress,
            cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await PublishUpdatedAsync(incident, cancellationToken);

        return IncidentResult<IncidentAttachmentDto>.Success(MapAttachment(attachment));
    }

    public async Task<IncidentResult<(IncidentAttachment Attachment, Stream Content)>> GetAttachmentAsync(
        Guid incidentId,
        Guid attachmentId,
        CancellationToken cancellationToken = default)
    {
        var attachment = await db.IncidentAttachments.AsNoTracking()
            .FirstOrDefaultAsync(
                a => a.Id == attachmentId &&
                     a.IncidentId == incidentId &&
                     a.OrganizationId == organizationContext.OrganizationId,
                cancellationToken);

        if (attachment is null)
        {
            return IncidentResult<(IncidentAttachment, Stream)>.Failure(
                new IncidentError(IncidentErrorCode.NotFound, "Attachment not found."));
        }

        try
        {
            var stream = await attachmentStorage.OpenReadAsync(attachment.StorageKey, cancellationToken);
            return IncidentResult<(IncidentAttachment, Stream)>.Success((attachment, stream));
        }
        catch (FileNotFoundException)
        {
            return IncidentResult<(IncidentAttachment, Stream)>.Failure(
                new IncidentError(IncidentErrorCode.NotFound, "Attachment file missing."));
        }
    }

    public async Task<IncidentResult<IncidentDto>> AssignAsync(
        Guid incidentId,
        AssignIncidentRequest request,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        var incident = await FindIncidentAsync(incidentId, tracking: true, cancellationToken);
        if (incident is null)
        {
            return NotFound();
        }

        var memberExists = await db.Memberships.AnyAsync(
            m => m.OrganizationId == organizationContext.OrganizationId &&
                 m.UserId == request.UserId,
            cancellationToken);
        if (!memberExists)
        {
            return Fail("Assignee must be a member of the organization.");
        }

        var oldValues = Snapshot(incident);
        incident.AssignedToUserId = request.UserId;
        if (incident.Status == IncidentStatus.Open)
        {
            incident.Status = IncidentStatus.Investigating;
        }

        incident.UpdatedAt = DateTimeOffset.UtcNow;
        AddSystemTimeline(incident, "Incident assigned", $"Assigned to user {request.UserId}");
        await WriteAuditAsync(incident, "IncidentAssigned", oldValues, Snapshot(incident), ipAddress, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await PublishUpdatedAsync(incident, cancellationToken);

        return IncidentResult<IncidentDto>.Success(await ToDtoAsync(incident, cancellationToken));
    }

    public async Task<IncidentResult<IncidentDto>> ResolveAsync(
        Guid incidentId,
        ResolveIncidentRequest request,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        var incident = await FindIncidentAsync(incidentId, tracking: true, cancellationToken);
        if (incident is null)
        {
            return NotFound();
        }

        var oldValues = Snapshot(incident);
        incident.Status = IncidentStatus.Resolved;
        incident.EndedAt = DateTimeOffset.UtcNow;
        incident.UpdatedAt = DateTimeOffset.UtcNow;

        var note = string.IsNullOrWhiteSpace(request.ResolutionNote)
            ? "Incident resolved."
            : request.ResolutionNote.Trim();
        AddSystemTimeline(incident, "Incident resolved", note);
        await WriteAuditAsync(incident, "IncidentResolved", oldValues, Snapshot(incident), ipAddress, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await PublishUpdatedAsync(incident, cancellationToken);

        return IncidentResult<IncidentDto>.Success(await ToDtoAsync(incident, cancellationToken));
    }

    public async Task<IncidentResult<IReadOnlyList<AuditLogDto>>> GetAuditAsync(
        Guid incidentId,
        CancellationToken cancellationToken = default)
    {
        if (await FindIncidentAsync(incidentId, tracking: false, cancellationToken) is null)
        {
            return IncidentResult<IReadOnlyList<AuditLogDto>>.Failure(
                new IncidentError(IncidentErrorCode.NotFound, "Incident not found."));
        }

        var logs = await db.AuditLogs.AsNoTracking()
            .Where(a => a.OrganizationId == organizationContext.OrganizationId &&
                        a.EntityType == "Incident" &&
                        a.EntityId == incidentId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(200)
            .ToListAsync(cancellationToken);

        return IncidentResult<IReadOnlyList<AuditLogDto>>.Success(logs.Select(MapAudit).ToList());
    }

    private async Task<Incident?> FindIncidentAsync(
        Guid incidentId,
        bool tracking,
        CancellationToken cancellationToken)
    {
        var query = tracking ? db.Incidents.AsQueryable() : db.Incidents.AsNoTracking();
        return await query.FirstOrDefaultAsync(
            i => i.Id == incidentId && i.OrganizationId == organizationContext.OrganizationId,
            cancellationToken);
    }

    private async Task<Dictionary<Guid, RiskAssessmentDto>> LoadLatestRiskAsync(
        List<Guid> incidentIds,
        CancellationToken cancellationToken)
    {
        if (incidentIds.Count == 0)
        {
            return new Dictionary<Guid, RiskAssessmentDto>();
        }

        var assessments = await db.RiskAssessments.AsNoTracking()
            .Where(r => incidentIds.Contains(r.IncidentId))
            .OrderByDescending(r => r.CalculatedAt)
            .ToListAsync(cancellationToken);

        return assessments
            .GroupBy(r => r.IncidentId)
            .ToDictionary(g => g.Key, g => MapRisk(g.First()));
    }

    private async Task<IncidentDto> ToDtoAsync(Incident incident, CancellationToken cancellationToken)
    {
        var count = await db.Detections.CountAsync(d => d.IncidentId == incident.Id, cancellationToken);
        var risk = await db.RiskAssessments.AsNoTracking()
            .Where(r => r.IncidentId == incident.Id)
            .OrderByDescending(r => r.CalculatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        return MapIncident(incident, count, risk is null ? null : MapRisk(risk));
    }

    private void AddSystemTimeline(Incident incident, string title, string description)
    {
        db.IncidentTimelineEntries.Add(new IncidentTimelineEntry
        {
            Id = Guid.NewGuid(),
            OrganizationId = incident.OrganizationId,
            IncidentId = incident.Id,
            EntryType = TimelineEntryType.System,
            Timestamp = DateTimeOffset.UtcNow,
            Title = title,
            Description = description,
            SourceType = "System",
            CreatedByUserId = organizationContext.UserId,
            CreatedAt = DateTimeOffset.UtcNow
        });
    }

    private async Task WriteAuditAsync(
        Incident incident,
        string action,
        string? oldValues,
        string? newValues,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            OrganizationId = incident.OrganizationId,
            UserId = organizationContext.UserId,
            Action = action,
            EntityType = "Incident",
            EntityId = incident.Id,
            OldValues = oldValues,
            NewValues = newValues,
            IpAddress = ipAddress,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await Task.CompletedTask;
    }

    private async Task PublishUpdatedAsync(Incident incident, CancellationToken cancellationToken)
    {
        await realtimePublisher.PublishIncidentUpdatedAsync(
            new IncidentRealtimeMessage(
                incident.OrganizationId,
                incident.Id,
                incident.PrimaryAssetId,
                incident.Title,
                incident.Status.ToString(),
                incident.RiskScore,
                incident.Severity.ToString(),
                incident.UpdatedAt),
            cancellationToken);
    }

    private static string Snapshot(Incident incident) =>
        JsonSerializer.Serialize(new
        {
            incident.Title,
            incident.Description,
            status = incident.Status.ToString(),
            severity = incident.Severity.ToString(),
            incident.RiskScore,
            incident.AssignedToUserId,
            incident.EndedAt
        });

    private static IncidentDto MapIncident(Incident i, int detectionCount, RiskAssessmentDto? risk) =>
        new(
            i.Id,
            i.PrimaryAssetId,
            i.Title,
            i.Description,
            i.IncidentType,
            i.Status,
            i.Severity,
            i.RiskScore,
            i.Confidence,
            i.StartedAt,
            i.EndedAt,
            i.DetectedAt,
            i.AssignedToUserId,
            i.CreatedAt,
            i.UpdatedAt,
            detectionCount,
            risk);

    private static RiskAssessmentDto MapRisk(RiskAssessment r) =>
        new(r.Id, r.Score, r.RiskLevel, r.Factors, r.ModelVersion, r.CalculatedAt);

    private static IncidentTimelineEntryDto MapTimeline(IncidentTimelineEntry e) =>
        new(
            e.Id,
            e.EntryType,
            e.Timestamp,
            e.Title,
            e.Description,
            e.SourceType,
            e.SourceId,
            e.Latitude,
            e.Longitude,
            e.Metadata,
            e.CreatedByUserId,
            e.CreatedAt);

    private static IncidentEntityDto MapEntity(IncidentEntity e) =>
        new(e.Id, e.EntityType, e.EntityId, e.RelationshipType, e.FirstObservedAt, e.LastObservedAt, e.Metadata);

    private static IncidentCommentDto MapComment(IncidentComment c) =>
        new(c.Id, c.UserId, c.Content, c.CreatedAt, c.UpdatedAt);

    private static IncidentAttachmentDto MapAttachment(IncidentAttachment a) =>
        new(a.Id, a.UploadedByUserId, a.Name, a.ContentType, a.Size, a.CreatedAt);

    private static AuditLogDto MapAudit(AuditLog a) =>
        new(a.Id, a.UserId, a.Action, a.EntityType, a.EntityId, a.OldValues, a.NewValues, a.IpAddress, a.CreatedAt);

    private static IncidentResult<IReadOnlyList<IncidentDto>> FailList(string message) =>
        IncidentResult<IReadOnlyList<IncidentDto>>.Failure(
            new IncidentError(IncidentErrorCode.Validation, message));

    private static IncidentResult<IncidentDto> Fail(string message) =>
        IncidentResult<IncidentDto>.Failure(new IncidentError(IncidentErrorCode.Validation, message));

    private static IncidentResult<IncidentDto> NotFound() =>
        IncidentResult<IncidentDto>.Failure(new IncidentError(IncidentErrorCode.NotFound, "Incident not found."));

    private static IncidentResult<IncidentDetailDto> NotFoundDetail() =>
        IncidentResult<IncidentDetailDto>.Failure(
            new IncidentError(IncidentErrorCode.NotFound, "Incident not found."));
}
