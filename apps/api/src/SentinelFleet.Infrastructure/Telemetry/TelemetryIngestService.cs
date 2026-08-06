using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SentinelFleet.Application.Telemetry;
using SentinelFleet.Domain.Devices;
using SentinelFleet.Infrastructure.Devices;
using SentinelFleet.Infrastructure.Persistence;

namespace SentinelFleet.Infrastructure.Telemetry;

public sealed class TelemetryIngestService(
    SentinelFleetDbContext db,
    ITelemetryQueuePublisher publisher) : ITelemetryIngestService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<TelemetryResult<IngestTelemetryAcceptedResponse>> IngestAsync(
        string apiKey,
        IngestTelemetryEventRequest request,
        CancellationToken cancellationToken = default)
    {
        var deviceResult = await ResolveDeviceAsync(apiKey, cancellationToken);
        if (!deviceResult.Succeeded)
        {
            return TelemetryResult<IngestTelemetryAcceptedResponse>.Failure(deviceResult.Error!);
        }

        var validationError = ValidateEvent(request);
        if (validationError is not null)
        {
            return TelemetryResult<IngestTelemetryAcceptedResponse>.Failure(validationError);
        }

        var receivedAt = DateTimeOffset.UtcNow;
        var message = ToQueuedMessage(deviceResult.Value!, request, receivedAt);
        await publisher.PublishAsync(message, cancellationToken);

        return TelemetryResult<IngestTelemetryAcceptedResponse>.Success(
            new IngestTelemetryAcceptedResponse(message.EventId, receivedAt));
    }

    public async Task<TelemetryResult<IngestTelemetryBatchResponse>> IngestBatchAsync(
        string apiKey,
        IngestTelemetryBatchRequest request,
        CancellationToken cancellationToken = default)
    {
        var deviceResult = await ResolveDeviceAsync(apiKey, cancellationToken);
        if (!deviceResult.Succeeded)
        {
            return TelemetryResult<IngestTelemetryBatchResponse>.Failure(deviceResult.Error!);
        }

        if (request.Events is null || request.Events.Count == 0)
        {
            return TelemetryResult<IngestTelemetryBatchResponse>.Failure(
                new TelemetryError(TelemetryErrorCode.Validation, "At least one event is required."));
        }

        if (request.Events.Count > 100)
        {
            return TelemetryResult<IngestTelemetryBatchResponse>.Failure(
                new TelemetryError(TelemetryErrorCode.Validation, "Batch size cannot exceed 100 events."));
        }

        var accepted = new List<IngestTelemetryAcceptedResponse>();
        var rejected = new List<IngestTelemetryBatchError>();
        var device = deviceResult.Value!;
        var receivedAt = DateTimeOffset.UtcNow;

        foreach (var evt in request.Events)
        {
            var validationError = ValidateEvent(evt);
            if (validationError is not null)
            {
                rejected.Add(new IngestTelemetryBatchError(evt.EventId, validationError.Message));
                continue;
            }

            var message = ToQueuedMessage(device, evt, receivedAt);
            await publisher.PublishAsync(message, cancellationToken);
            accepted.Add(new IngestTelemetryAcceptedResponse(message.EventId, receivedAt));
        }

        return TelemetryResult<IngestTelemetryBatchResponse>.Success(
            new IngestTelemetryBatchResponse(accepted, rejected));
    }

    private async Task<TelemetryResult<Device>> ResolveDeviceAsync(
        string apiKey,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return TelemetryResult<Device>.Failure(
                new TelemetryError(TelemetryErrorCode.Unauthorized, "API key is required."));
        }

        var hash = DeviceApiKeyHasher.Hash(apiKey.Trim());
        var device = await db.Devices.FirstOrDefaultAsync(d => d.ApiKeyHash == hash, cancellationToken);

        if (device is null)
        {
            return TelemetryResult<Device>.Failure(
                new TelemetryError(TelemetryErrorCode.Unauthorized, "Invalid API key."));
        }

        if (device.Status is DeviceStatus.Revoked or DeviceStatus.Inactive)
        {
            return TelemetryResult<Device>.Failure(
                new TelemetryError(TelemetryErrorCode.Unauthorized, "Device is not active."));
        }

        if (device.AssetId is null)
        {
            return TelemetryResult<Device>.Failure(
                new TelemetryError(TelemetryErrorCode.Validation, "Device is not linked to an asset."));
        }

        return TelemetryResult<Device>.Success(device);
    }

    private static TelemetryError? ValidateEvent(IngestTelemetryEventRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.EventId))
        {
            return new TelemetryError(TelemetryErrorCode.Validation, "eventId is required.");
        }

        if (request.EventId.Length > 64)
        {
            return new TelemetryError(TelemetryErrorCode.Validation, "eventId must be at most 64 characters.");
        }

        if (string.IsNullOrWhiteSpace(request.EventType))
        {
            return new TelemetryError(TelemetryErrorCode.Validation, "eventType is required.");
        }

        if (request.Position is null)
        {
            return new TelemetryError(TelemetryErrorCode.Validation, "position is required.");
        }

        if (request.Position.Latitude is < -90 or > 90)
        {
            return new TelemetryError(TelemetryErrorCode.Validation, "latitude must be between -90 and 90.");
        }

        if (request.Position.Longitude is < -180 or > 180)
        {
            return new TelemetryError(TelemetryErrorCode.Validation, "longitude must be between -180 and 180.");
        }

        if (request.SchemaVersion < 1)
        {
            return new TelemetryError(TelemetryErrorCode.Validation, "schemaVersion must be >= 1.");
        }

        return null;
    }

    private static QueuedTelemetryMessage ToQueuedMessage(
        Device device,
        IngestTelemetryEventRequest request,
        DateTimeOffset receivedAt)
    {
        var rawPayload = JsonSerializer.Serialize(request, JsonOptions);

        return new QueuedTelemetryMessage(
            EventId: request.EventId.Trim(),
            OrganizationId: device.OrganizationId,
            AssetId: device.AssetId!.Value,
            DeviceId: device.Id,
            EventType: request.EventType.Trim(),
            RecordedAt: request.RecordedAt,
            ReceivedAt: receivedAt,
            SchemaVersion: request.SchemaVersion,
            Latitude: request.Position.Latitude,
            Longitude: request.Position.Longitude,
            SpeedKph: request.Position.SpeedKph,
            Heading: request.Position.Heading,
            IgnitionOn: request.Vehicle?.IgnitionOn,
            OdometerKm: request.Vehicle?.OdometerKm,
            FuelLevelPercent: request.Vehicle?.FuelLevelPercent,
            DriverUserId: request.Driver?.UserId,
            RawPayload: rawPayload);
    }
}
