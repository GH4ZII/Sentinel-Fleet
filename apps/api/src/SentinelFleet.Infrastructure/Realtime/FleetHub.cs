using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SentinelFleet.Application.Security;

namespace SentinelFleet.Infrastructure.Realtime;

[Authorize]
public sealed class FleetHub : Hub
{
    public static string OrgGroup(Guid organizationId) => $"org:{organizationId}";

    public override async Task OnConnectedAsync()
    {
        var orgClaim = Context.User?.FindFirst(AuthClaimTypes.OrganizationId)?.Value;
        if (Guid.TryParse(orgClaim, out var organizationId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, OrgGroup(organizationId));
        }

        await base.OnConnectedAsync();
    }
}
