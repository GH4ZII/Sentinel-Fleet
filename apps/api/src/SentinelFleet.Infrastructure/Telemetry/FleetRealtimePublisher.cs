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

    public async Task PublishDetectionCreatedAsync(
        DetectionCreatedMessage message,
        CancellationToken cancellationToken = default)
    {
        await hubContext.Clients
            .Group(FleetHub.OrgGroup(message.OrganizationId))
            .SendAsync(
                "DetectionCreated",
                new
                {
                    id = message.DetectionId,
                    assetId = message.AssetId,
                    detectionType = message.DetectionType,
                    severity = message.Severity,
                    title = message.Title,
                    triggeredAt = message.TriggeredAt
                },
                cancellationToken);

        logger.LogDebug(
            "Pushed DetectionCreated {DetectionId} for asset {AssetId} to org {OrganizationId}",
            message.DetectionId,
            message.AssetId,
            message.OrganizationId);
    }
}
