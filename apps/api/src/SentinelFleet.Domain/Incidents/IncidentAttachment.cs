namespace SentinelFleet.Domain.Incidents;

public sealed class IncidentAttachment
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid IncidentId { get; set; }

    public Guid UploadedByUserId { get; set; }

    public required string Name { get; set; }

    public required string ContentType { get; set; }

    public required string StorageKey { get; set; }

    public long Size { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
