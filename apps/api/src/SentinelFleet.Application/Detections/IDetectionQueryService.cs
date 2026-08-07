using SentinelFleet.Domain.Rules;

namespace SentinelFleet.Application.Detections;

public interface IDetectionQueryService
{
    Task<DetectionResult<IReadOnlyList<DetectionDto>>> ListAsync(
        Guid? assetId = null,
        DetectionRuleType? detectionType = null,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        int limit = 100,
        CancellationToken cancellationToken = default);

    Task<DetectionResult<DetectionDto>> GetAsync(
        Guid detectionId,
        CancellationToken cancellationToken = default);
}

public class DetectionResult
{
    public bool Succeeded { get; init; }

    public DetectionError? Error { get; init; }

    public static DetectionResult Success() => new() { Succeeded = true };

    public static DetectionResult Failure(DetectionError error) =>
        new() { Succeeded = false, Error = error };
}

public sealed class DetectionResult<T> : DetectionResult
{
    public T? Value { get; init; }

    public static DetectionResult<T> Success(T value) =>
        new() { Succeeded = true, Value = value };

    public static new DetectionResult<T> Failure(DetectionError error) =>
        new() { Succeeded = false, Error = error };
}

public sealed record DetectionError(DetectionErrorCode Code, string Message);

public enum DetectionErrorCode
{
    Validation,
    NotFound,
    Forbidden
}
