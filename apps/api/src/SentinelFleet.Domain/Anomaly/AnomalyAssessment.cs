namespace SentinelFleet.Domain.Anomaly;

public sealed class AnomalyAssessment
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid AssetId { get; set; }

    public Guid? TelemetryEventId { get; set; }

    public Guid? IncidentId { get; set; }

    public double Score { get; set; }

    public double Confidence { get; set; }

    public required string ModelVersion { get; set; }

    public required string Method { get; set; }

    /// <summary>JSON array of feature names.</summary>
    public string? FeaturesUsed { get; set; }

    public required string Explanation { get; set; }

    public bool IsAnomaly { get; set; }

    public DateTimeOffset CalculatedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
