using SentinelFleet.Domain.Rules;

namespace SentinelFleet.Application.Rules;

public sealed record DetectionRuleDto(
    Guid Id,
    string Name,
    DetectionRuleType RuleType,
    string? Description,
    string? Configuration,
    DetectionSeverity Severity,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CreateDetectionRuleRequest(
    string Name,
    DetectionRuleType RuleType,
    string? Description,
    string? Configuration,
    DetectionSeverity Severity = DetectionSeverity.Medium,
    bool IsActive = true);

public sealed record UpdateDetectionRuleRequest(
    string? Name,
    string? Description,
    string? Configuration,
    DetectionSeverity? Severity,
    bool? IsActive);
