using SentinelFleet.Application.Identity;

namespace SentinelFleet.Application.Organizations;

public interface IInvitationService
{
    Task<OrganizationResult<InvitationCreatedDto>> CreateAsync(
        CreateInvitationRequest request,
        CancellationToken cancellationToken = default);

    Task<OrganizationResult<InvitationInfoDto>> GetAsync(
        string token,
        CancellationToken cancellationToken = default);

    Task<OrganizationResult<AuthResponse>> AcceptAsync(
        string token,
        AcceptInvitationRequest request,
        CancellationToken cancellationToken = default);
}
