using SentinelFleet.Domain.Organizations;

namespace SentinelFleet.Application.Organizations;

public sealed record OrganizationDto(
    Guid Id,
    string Name,
    string? OrganizationNumber,
    DateTimeOffset CreatedAt,
    string Settings);

public sealed record UpdateOrganizationRequest(
    string? Name,
    string? OrganizationNumber,
    string? Settings);

public sealed record MemberDto(
    Guid Id,
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    OrganizationRole Role,
    MembershipStatus Status,
    DateTimeOffset CreatedAt);

public sealed record UpdateMemberRequest(
    OrganizationRole? Role,
    MembershipStatus? Status);
