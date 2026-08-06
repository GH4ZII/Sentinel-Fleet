using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using SentinelFleet.Application.Devices;
using SentinelFleet.Application.Security;
using SentinelFleet.Domain.Devices;
using SentinelFleet.Infrastructure.Persistence;

namespace SentinelFleet.Infrastructure.Devices;

public sealed class DeviceService(
    SentinelFleetDbContext db,
    IOrganizationContext organizationContext) : IDeviceService
{
    public async Task<DeviceResult<IReadOnlyList<DeviceDto>>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        var devices = await db.Devices.AsNoTracking()
            .Where(d => d.OrganizationId == organizationContext.OrganizationId)
            .OrderBy(d => d.ExternalDeviceId)
            .ToListAsync(cancellationToken);

        return DeviceResult<IReadOnlyList<DeviceDto>>.Success(devices.Select(ToDto).ToList());
    }

    public async Task<DeviceResult<DeviceDto>> GetAsync(
        Guid deviceId,
        CancellationToken cancellationToken = default)
    {
        var device = await FindInOrgAsync(deviceId, cancellationToken);
        if (device is null)
        {
            return DeviceResult<DeviceDto>.Failure(
                new DeviceError(DeviceErrorCode.NotFound, "Device not found."));
        }

        return DeviceResult<DeviceDto>.Success(ToDto(device));
    }

    public async Task<DeviceResult<CreateDeviceResponse>> CreateAsync(
        CreateDeviceRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!organizationContext.CanMutate)
        {
            return DeviceResult<CreateDeviceResponse>.Failure(
                new DeviceError(DeviceErrorCode.Forbidden, "Insufficient permissions."));
        }

        if (string.IsNullOrWhiteSpace(request.ExternalDeviceId) ||
            string.IsNullOrWhiteSpace(request.DeviceType))
        {
            return DeviceResult<CreateDeviceResponse>.Failure(
                new DeviceError(DeviceErrorCode.Validation, "ExternalDeviceId and DeviceType are required."));
        }

        if (request.AssetId is Guid assetId)
        {
            var assetOk = await db.Assets.AnyAsync(
                a => a.Id == assetId && a.OrganizationId == organizationContext.OrganizationId,
                cancellationToken);
            if (!assetOk)
            {
                return DeviceResult<CreateDeviceResponse>.Failure(
                    new DeviceError(DeviceErrorCode.Validation, "Asset not found."));
            }
        }

        var apiKey = GenerateApiKey();
        var device = new Device
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationContext.OrganizationId,
            AssetId = request.AssetId,
            ExternalDeviceId = request.ExternalDeviceId.Trim(),
            DeviceType = request.DeviceType.Trim(),
            Status = DeviceStatus.Active,
            FirmwareVersion = request.FirmwareVersion?.Trim(),
            ApiKeyHash = DeviceApiKeyHasher.Hash(apiKey),
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.Devices.Add(device);
        await db.SaveChangesAsync(cancellationToken);

        return DeviceResult<CreateDeviceResponse>.Success(
            new CreateDeviceResponse(ToDto(device), apiKey));
    }

    public async Task<DeviceResult<DeviceDto>> UpdateAsync(
        Guid deviceId,
        UpdateDeviceRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!organizationContext.CanMutate)
        {
            return DeviceResult<DeviceDto>.Failure(
                new DeviceError(DeviceErrorCode.Forbidden, "Insufficient permissions."));
        }

        var device = await db.Devices.FirstOrDefaultAsync(
            d => d.Id == deviceId && d.OrganizationId == organizationContext.OrganizationId,
            cancellationToken);

        if (device is null)
        {
            return DeviceResult<DeviceDto>.Failure(
                new DeviceError(DeviceErrorCode.NotFound, "Device not found."));
        }

        if (request.AssetId is Guid assetId)
        {
            var assetOk = await db.Assets.AnyAsync(
                a => a.Id == assetId && a.OrganizationId == organizationContext.OrganizationId,
                cancellationToken);
            if (!assetOk)
            {
                return DeviceResult<DeviceDto>.Failure(
                    new DeviceError(DeviceErrorCode.Validation, "Asset not found."));
            }

            device.AssetId = assetId;
        }

        if (!string.IsNullOrWhiteSpace(request.DeviceType))
        {
            device.DeviceType = request.DeviceType.Trim();
        }

        if (request.Status is not null)
        {
            device.Status = request.Status.Value;
        }

        if (request.FirmwareVersion is not null)
        {
            device.FirmwareVersion = string.IsNullOrWhiteSpace(request.FirmwareVersion)
                ? null
                : request.FirmwareVersion.Trim();
        }

        await db.SaveChangesAsync(cancellationToken);
        return DeviceResult<DeviceDto>.Success(ToDto(device));
    }

    public async Task<DeviceResult<RotateDeviceKeyResponse>> RotateKeyAsync(
        Guid deviceId,
        CancellationToken cancellationToken = default)
    {
        if (!organizationContext.CanMutate)
        {
            return DeviceResult<RotateDeviceKeyResponse>.Failure(
                new DeviceError(DeviceErrorCode.Forbidden, "Insufficient permissions."));
        }

        var device = await db.Devices.FirstOrDefaultAsync(
            d => d.Id == deviceId && d.OrganizationId == organizationContext.OrganizationId,
            cancellationToken);

        if (device is null)
        {
            return DeviceResult<RotateDeviceKeyResponse>.Failure(
                new DeviceError(DeviceErrorCode.NotFound, "Device not found."));
        }

        var apiKey = GenerateApiKey();
        device.ApiKeyHash = DeviceApiKeyHasher.Hash(apiKey);
        await db.SaveChangesAsync(cancellationToken);

        return DeviceResult<RotateDeviceKeyResponse>.Success(
            new RotateDeviceKeyResponse(device.Id, apiKey));
    }

    private async Task<Device?> FindInOrgAsync(Guid deviceId, CancellationToken cancellationToken)
    {
        return await db.Devices.AsNoTracking()
            .FirstOrDefaultAsync(
                d => d.Id == deviceId && d.OrganizationId == organizationContext.OrganizationId,
                cancellationToken);
    }

    private static DeviceDto ToDto(Device d) => new(
        d.Id,
        d.AssetId,
        d.ExternalDeviceId,
        d.DeviceType,
        d.Status,
        d.LastSeenAt,
        d.FirmwareVersion,
        d.CreatedAt);

    private static string GenerateApiKey()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes);
    }
}
