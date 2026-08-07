namespace SentinelFleet.Domain.Incidents;

public sealed class IncidentEntity
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid IncidentId { get; set; }

    public required string EntityType { get; set; }

    public Guid EntityId { get; set; }

    public required string RelationshipType { get; set; }

    public DateTimeOffset FirstObservedAt { get; set; }

    public DateTimeOffset LastObservedAt { get; set; }

    public string? Metadata { get; set; }
}
