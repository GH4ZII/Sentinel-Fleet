using SentinelFleet.Domain.Incidents;

namespace SentinelFleet.Application.Incidents;

public interface IIncidentService
{
    Task<IncidentResult<IReadOnlyList<IncidentDto>>> ListAsync(
        Guid? assetId = null,
        IncidentStatus? status = null,
        int limit = 100,
        CancellationToken cancellationToken = default);

    Task<IncidentResult<IncidentDetailDto>> GetAsync(
        Guid incidentId,
        CancellationToken cancellationToken = default);

    Task<IncidentResult<IncidentDto>> UpdateAsync(
        Guid incidentId,
        UpdateIncidentRequest request,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    Task<IncidentResult<IReadOnlyList<IncidentTimelineEntryDto>>> GetTimelineAsync(
        Guid incidentId,
        TimelineEntryType? entryType = null,
        CancellationToken cancellationToken = default);

    Task<IncidentResult<IReadOnlyList<IncidentEntityDto>>> GetRelationshipsAsync(
        Guid incidentId,
        CancellationToken cancellationToken = default);

    Task<IncidentResult<IReadOnlyList<IncidentPositionDto>>> GetPositionsAsync(
        Guid incidentId,
        CancellationToken cancellationToken = default);

    Task<IncidentResult<IncidentCommentDto>> AddCommentAsync(
        Guid incidentId,
        AddIncidentCommentRequest request,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    Task<IncidentResult<IncidentAttachmentDto>> AddAttachmentAsync(
        Guid incidentId,
        string fileName,
        string contentType,
        Stream content,
        long size,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    Task<IncidentResult<(IncidentAttachment Attachment, Stream Content)>> GetAttachmentAsync(
        Guid incidentId,
        Guid attachmentId,
        CancellationToken cancellationToken = default);

    Task<IncidentResult<IncidentDto>> AssignAsync(
        Guid incidentId,
        AssignIncidentRequest request,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    Task<IncidentResult<IncidentDto>> ResolveAsync(
        Guid incidentId,
        ResolveIncidentRequest request,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    Task<IncidentResult<IReadOnlyList<AuditLogDto>>> GetAuditAsync(
        Guid incidentId,
        CancellationToken cancellationToken = default);
}

public class IncidentResult
{
    public bool Succeeded { get; init; }

    public IncidentError? Error { get; init; }

    public static IncidentResult Success() => new() { Succeeded = true };

    public static IncidentResult Failure(IncidentError error) =>
        new() { Succeeded = false, Error = error };
}

public sealed class IncidentResult<T> : IncidentResult
{
    public T? Value { get; init; }

    public static IncidentResult<T> Success(T value) =>
        new() { Succeeded = true, Value = value };

    public static new IncidentResult<T> Failure(IncidentError error) =>
        new() { Succeeded = false, Error = error };
}

public sealed record IncidentError(IncidentErrorCode Code, string Message);

public enum IncidentErrorCode
{
    Validation,
    NotFound,
    Forbidden
}
