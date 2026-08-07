namespace SentinelFleet.Domain.Incidents;

public sealed class IncidentComment
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid IncidentId { get; set; }

    public Guid UserId { get; set; }

    public required string Content { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
