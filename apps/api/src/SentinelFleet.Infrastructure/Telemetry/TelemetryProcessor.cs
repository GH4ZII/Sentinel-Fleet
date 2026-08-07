using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SentinelFleet.Application.Rules;
using SentinelFleet.Application.Telemetry;
using SentinelFleet.Domain.Telemetry;
using SentinelFleet.Infrastructure.Persistence;

namespace SentinelFleet.Infrastructure.Telemetry;

public sealed class TelemetryProcessor(
    SentinelFleetDbContext db,
    IFleetRealtimePublisher realtimePublisher,
    IRuleEngine ruleEngine,
    ILogger<TelemetryProcessor> logger) : ITelemetryProcessor
{
    public async Task ProcessAsync(QueuedTelemetryMessage message, CancellationToken cancellationToken = default)
    {
        var exists = await db.TelemetryEvents.AnyAsync(
            e => e.EventId == message.EventId,
            cancellationToken);

        if (exists)
        {
            logger.LogDebug("Skipping duplicate telemetry event {EventId}", message.EventId);
            return;
        }

        db.TelemetryEvents.Add(new TelemetryEvent
        {
            Id = Guid.NewGuid(),
            EventId = message.EventId,
            OrganizationId = message.OrganizationId,
            AssetId = message.AssetId,
            DeviceId = message.DeviceId,
            EventType = message.EventType,
            RecordedAt = message.RecordedAt,
            ReceivedAt = message.ReceivedAt,
            SchemaVersion = message.SchemaVersion,
            Latitude = message.Latitude,
            Longitude = message.Longitude,
            SpeedKph = message.SpeedKph,
            Heading = message.Heading,
            IgnitionOn = message.IgnitionOn,
            OdometerKm = message.OdometerKm,
            FuelLevelPercent = message.FuelLevelPercent,
            DriverUserId = message.DriverUserId,
            RawPayload = message.RawPayload
        });

        var asset = await db.Assets.FirstOrDefaultAsync(
            a => a.Id == message.AssetId && a.OrganizationId == message.OrganizationId,
            cancellationToken);

        if (asset is not null)
        {
            asset.MapLatitude = message.Latitude;
            asset.MapLongitude = message.Longitude;
            asset.UpdatedAt = DateTimeOffset.UtcNow;
        }

        var device = await db.Devices.FirstOrDefaultAsync(
            d => d.Id == message.DeviceId,
            cancellationToken);

        if (device is not null)
        {
            device.LastSeenAt = message.ReceivedAt;
        }

        await db.SaveChangesAsync(cancellationToken);

        await realtimePublisher.PublishPositionUpdatedAsync(
            new PositionUpdatedMessage(
                message.OrganizationId,
                message.AssetId,
                message.Latitude,
                message.Longitude,
                message.SpeedKph,
                message.Heading,
                message.RecordedAt),
            cancellationToken);

        try
        {
            await ruleEngine.EvaluateTelemetryAsync(message, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Rule engine failed for telemetry event {EventId} asset {AssetId}",
                message.EventId,
                message.AssetId);
        }
    }
}
