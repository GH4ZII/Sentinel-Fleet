namespace SentinelFleet.Application.Rules;

public interface IDetectionRuleService
{
    Task<RuleResult<IReadOnlyList<DetectionRuleDto>>> ListAsync(
        CancellationToken cancellationToken = default);

    Task<RuleResult<DetectionRuleDto>> GetAsync(
        Guid ruleId,
        CancellationToken cancellationToken = default);

    Task<RuleResult<DetectionRuleDto>> CreateAsync(
        CreateDetectionRuleRequest request,
        CancellationToken cancellationToken = default);

    Task<RuleResult<DetectionRuleDto>> UpdateAsync(
        Guid ruleId,
        UpdateDetectionRuleRequest request,
        CancellationToken cancellationToken = default);

    Task<RuleResult<DetectionRuleDto>> EnableAsync(
        Guid ruleId,
        CancellationToken cancellationToken = default);

    Task<RuleResult<DetectionRuleDto>> DisableAsync(
        Guid ruleId,
        CancellationToken cancellationToken = default);

    Task EnsureDefaultRulesAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);
}

public class RuleResult
{
    public bool Succeeded { get; init; }

    public RuleError? Error { get; init; }

    public static RuleResult Success() => new() { Succeeded = true };

    public static RuleResult Failure(RuleError error) =>
        new() { Succeeded = false, Error = error };
}

public sealed class RuleResult<T> : RuleResult
{
    public T? Value { get; init; }

    public static RuleResult<T> Success(T value) =>
        new() { Succeeded = true, Value = value };

    public static new RuleResult<T> Failure(RuleError error) =>
        new() { Succeeded = false, Error = error };
}

public sealed record RuleError(RuleErrorCode Code, string Message);

public enum RuleErrorCode
{
    Validation,
    NotFound,
    Forbidden
}
