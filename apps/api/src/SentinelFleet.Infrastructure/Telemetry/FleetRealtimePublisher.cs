using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SentinelFleet.Application.Telemetry;
using SentinelFleet.Infrastructure.Realtime;

namespace SentinelFleet.Infrastructure.Telemetry;

public sealed class FleetRealtimePublisher(
    IHubContext<FleetHub> hubContext,
    ILogger<FleetRealtimePublisher> logger) : IFleetRealtimePublisher
{
    public async Task PublishPositionUpdatedAsync(
        PositionUpdatedMessage message,
        CancellationToken cancellationToken = default)
    {
        await hubContext.Clients
            .Group(FleetHub.OrgGroup(message.OrganizationId))
            .SendAsync(
                "PositionUpdated",
                new
                {
                    assetId = message.AssetId,
                    latitude = message.Latitude,
                    longitude = message.Longitude,
                    speedKph = message.SpeedKph,
                    heading = message.Heading,
                    recordedAt = message.RecordedAt
                },
                cancellationToken);

        logger.LogDebug(
            "Pushed PositionUpdated for asset {AssetId} to org {OrganizationId}",
            message.AssetId,
            message.OrganizationId);
    }
}
