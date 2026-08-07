using SentinelFleet.Domain.Rules;

namespace SentinelFleet.Domain.Detections;

public sealed class Detection
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid AssetId { get; set; }

    public Guid? RuleId { get; set; }

    public DetectionRuleType DetectionType { get; set; }

    public DetectionSeverity Severity { get; set; }

    public double Confidence { get; set; } = 1.0;

    public int RiskContribution { get; set; }

    public required string Title { get; set; }

    public string? Description { get; set; }

    public DateTimeOffset TriggeredAt { get; set; }

    /// <summary>JSON array of source telemetry event ids.</summary>
    public string? SourceEventIds { get; set; }

    /// <summary>JSON metadata bag.</summary>
    public string? Metadata { get; set; }

    /// <summary>Set in Week 5 when incidents are correlated.</summary>
    public Guid? IncidentId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
