namespace SentinelFleet.Domain.Incidents;

public sealed class Incident
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid PrimaryAssetId { get; set; }

    public required string Title { get; set; }

    public string? Description { get; set; }

    public IncidentType IncidentType { get; set; }

    public IncidentStatus Status { get; set; } = IncidentStatus.Open;

    public IncidentSeverity Severity { get; set; }

    public int RiskScore { get; set; }

    public double Confidence { get; set; } = 0.8;

    public DateTimeOffset StartedAt { get; set; }

    public DateTimeOffset? EndedAt { get; set; }

    public DateTimeOffset DetectedAt { get; set; }

    public Guid? AssignedToUserId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
