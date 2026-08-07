namespace SentinelFleet.Domain.Incidents;

public sealed class IncidentTimelineEntry
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid IncidentId { get; set; }

    public TimelineEntryType EntryType { get; set; }

    public DateTimeOffset Timestamp { get; set; }

    public required string Title { get; set; }

    public string? Description { get; set; }

    public string? SourceType { get; set; }

    public Guid? SourceId { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public string? Metadata { get; set; }

    public Guid? CreatedByUserId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
