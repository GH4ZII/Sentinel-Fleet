using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SentinelFleet.Application.Security;
using SentinelFleet.Domain.Organizations;

namespace SentinelFleet.Infrastructure.Security;

public sealed class OrganizationContext(IHttpContextAccessor httpContextAccessor) : IOrganizationContext
{
    private ClaimsPrincipal Principal =>
        httpContextAccessor.HttpContext?.User
        ?? throw new InvalidOperationException("No HTTP context available.");

    public Guid UserId
    {
        get
        {
            var value = Principal.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? Principal.FindFirstValue("sub");
            if (!Guid.TryParse(value, out var id))
            {
                throw new InvalidOperationException("User id claim is missing.");
            }

            return id;
        }
    }

    public Guid OrganizationId
    {
        get
        {
            var value = Principal.FindFirstValue(AuthClaimTypes.OrganizationId);
            if (!Guid.TryParse(value, out var id))
            {
                throw new InvalidOperationException("Organization id claim is missing.");
            }

            return id;
        }
    }

    public OrganizationRole Role
    {
        get
        {
            var value = Principal.FindFirstValue(AuthClaimTypes.Role)
                ?? Principal.FindFirstValue(ClaimTypes.Role);
            if (!Enum.TryParse<OrganizationRole>(value, ignoreCase: true, out var role))
            {
                throw new InvalidOperationException("Organization role claim is missing.");
            }

            return role;
        }
    }

    public bool CanMutate =>
        Role is OrganizationRole.Owner or OrganizationRole.SecurityManager;

    public bool IsOwner => Role == OrganizationRole.Owner;
}
