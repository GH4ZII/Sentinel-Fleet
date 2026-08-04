namespace SentinelFleet.Application.Organizations;

public interface IOrganizationService
{
    Task<OrganizationResult<OrganizationDto>> GetCurrentAsync(CancellationToken cancellationToken = default);

    Task<OrganizationResult<OrganizationDto>> UpdateCurrentAsync(
        UpdateOrganizationRequest request,
        CancellationToken cancellationToken = default);

    Task<OrganizationResult<IReadOnlyList<MemberDto>>> ListMembersAsync(
        CancellationToken cancellationToken = default);

    Task<OrganizationResult<MemberDto>> UpdateMemberAsync(
        Guid memberId,
        UpdateMemberRequest request,
        CancellationToken cancellationToken = default);
}

public class OrganizationResult
{
    public bool Succeeded { get; init; }

    public OrganizationError? Error { get; init; }

    public static OrganizationResult Success() => new() { Succeeded = true };

    public static OrganizationResult Failure(OrganizationError error) =>
        new() { Succeeded = false, Error = error };
}

public sealed class OrganizationResult<T> : OrganizationResult
{
    public T? Value { get; init; }

    public static OrganizationResult<T> Success(T value) => new() { Succeeded = true, Value = value };

    public static new OrganizationResult<T> Failure(OrganizationError error) =>
        new() { Succeeded = false, Error = error };
}

public sealed record OrganizationError(OrganizationErrorCode Code, string Message);

public enum OrganizationErrorCode
{
    Validation,
    NotFound,
    Forbidden,
    Conflict
}
