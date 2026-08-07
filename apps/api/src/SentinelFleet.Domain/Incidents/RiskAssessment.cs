namespace SentinelFleet.Domain.Incidents;

public sealed class RiskAssessment
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid IncidentId { get; set; }

    public int Score { get; set; }

    public RiskLevel RiskLevel { get; set; }

    /// <summary>JSON array of scoring factors with explanations.</summary>
    public required string Factors { get; set; }

    public required string ModelVersion { get; set; }

    public DateTimeOffset CalculatedAt { get; set; }
}
