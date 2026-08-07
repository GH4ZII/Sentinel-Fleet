using SentinelFleet.Domain.Rules;

namespace SentinelFleet.Application.Detections;

public sealed record DetectionDto(
    Guid Id,
    Guid AssetId,
    Guid? RuleId,
    DetectionRuleType DetectionType,
    DetectionSeverity Severity,
    double Confidence,
    int RiskContribution,
    string Title,
    string? Description,
    DateTimeOffset TriggeredAt,
    string? SourceEventIds,
    string? Metadata,
    Guid? IncidentId,
    DateTimeOffset CreatedAt);
