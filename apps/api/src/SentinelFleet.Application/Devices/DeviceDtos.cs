using SentinelFleet.Domain.Devices;

namespace SentinelFleet.Application.Devices;

public sealed record DeviceDto(
    Guid Id,
    Guid? AssetId,
    string ExternalDeviceId,
    string DeviceType,
    DeviceStatus Status,
    DateTimeOffset? LastSeenAt,
    string? FirmwareVersion,
    DateTimeOffset CreatedAt);

public sealed record CreateDeviceRequest(
    Guid? AssetId,
    string ExternalDeviceId,
    string DeviceType,
    string? FirmwareVersion);

public sealed record UpdateDeviceRequest(
    Guid? AssetId,
    string? DeviceType,
    DeviceStatus? Status,
    string? FirmwareVersion);

public sealed record CreateDeviceResponse(
    DeviceDto Device,
    string ApiKey);

public sealed record RotateDeviceKeyResponse(
    Guid DeviceId,
    string ApiKey);
