using SentinelFleet.Domain.Incidents;

namespace SentinelFleet.Application.Incidents;

public sealed record IncidentDto(
    Guid Id,
    Guid PrimaryAssetId,
    string Title,
    string? Description,
    IncidentType IncidentType,
    IncidentStatus Status,
    IncidentSeverity Severity,
    int RiskScore,
    double Confidence,
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt,
    DateTimeOffset DetectedAt,
    Guid? AssignedToUserId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int DetectionCount,
    RiskAssessmentDto? LatestRisk);

public sealed record RiskAssessmentDto(
    Guid Id,
    int Score,
    RiskLevel RiskLevel,
    string Factors,
    string ModelVersion,
    DateTimeOffset CalculatedAt);

public sealed record IncidentTimelineEntryDto(
    Guid Id,
    TimelineEntryType EntryType,
    DateTimeOffset Timestamp,
    string Title,
    string? Description,
    string? SourceType,
    Guid? SourceId,
    double? Latitude,
    double? Longitude,
    string? Metadata,
    Guid? CreatedByUserId,
    DateTimeOffset CreatedAt);

public sealed record IncidentEntityDto(
    Guid Id,
    string EntityType,
    Guid EntityId,
    string RelationshipType,
    DateTimeOffset FirstObservedAt,
    DateTimeOffset LastObservedAt,
    string? Metadata);

public sealed record IncidentCommentDto(
    Guid Id,
    Guid UserId,
    string Content,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record IncidentAttachmentDto(
    Guid Id,
    Guid UploadedByUserId,
    string Name,
    string ContentType,
    long Size,
    DateTimeOffset CreatedAt);

public sealed record IncidentPositionDto(
    string EventId,
    double Latitude,
    double Longitude,
    double? SpeedKph,
    double? Heading,
    DateTimeOffset RecordedAt);

public sealed record IncidentDetailDto(
    IncidentDto Incident,
    IReadOnlyList<DetectionSummaryDto> Detections,
    IReadOnlyList<IncidentTimelineEntryDto> Timeline,
    IReadOnlyList<IncidentEntityDto> Relationships,
    IReadOnlyList<IncidentCommentDto> Comments,
    IReadOnlyList<IncidentAttachmentDto> Attachments,
    IReadOnlyList<AuditLogDto> Audit);

public sealed record DetectionSummaryDto(
    Guid Id,
    string DetectionType,
    string Severity,
    int RiskContribution,
    string Title,
    string? Description,
    DateTimeOffset TriggeredAt,
    string? Metadata);

public sealed record AuditLogDto(
    Guid Id,
    Guid? UserId,
    string Action,
    string EntityType,
    Guid EntityId,
    string? OldValues,
    string? NewValues,
    string? IpAddress,
    DateTimeOffset CreatedAt);

public sealed record UpdateIncidentRequest(
    IncidentStatus? Status,
    string? Title,
    string? Description);

public sealed record AssignIncidentRequest(Guid UserId);

public sealed record AddIncidentCommentRequest(string Content);

public sealed record ResolveIncidentRequest(string? ResolutionNote);
