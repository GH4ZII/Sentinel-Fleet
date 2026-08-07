namespace SentinelFleet.Domain.Rules;

public sealed class DetectionRule
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public required string Name { get; set; }

    public DetectionRuleType RuleType { get; set; }

    public string? Description { get; set; }

    /// <summary>JSON configuration for thresholds and windows.</summary>
    public string? Configuration { get; set; }

    public DetectionSeverity Severity { get; set; } = DetectionSeverity.Medium;

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
