using SentinelFleet.Domain.Organizations;

namespace SentinelFleet.Application.Organizations;

public sealed record CreateInvitationRequest(
    string Email,
    OrganizationRole Role);

public sealed record InvitationCreatedDto(
    Guid InvitationId,
    string Email,
    OrganizationRole Role,
    string Token,
    DateTimeOffset ExpiresAt,
    string AcceptUrl);

public sealed record InvitationInfoDto(
    string EmailMasked,
    string OrganizationName,
    OrganizationRole Role,
    DateTimeOffset ExpiresAt);

public sealed record AcceptInvitationRequest(
    string FirstName,
    string LastName,
    string Password);
