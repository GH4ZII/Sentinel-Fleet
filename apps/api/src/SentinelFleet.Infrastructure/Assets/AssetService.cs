using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using SentinelFleet.Application.Assets;
using SentinelFleet.Application.Security;
using SentinelFleet.Domain.Assets;
using SentinelFleet.Domain.Devices;
using SentinelFleet.Infrastructure.Devices;
using SentinelFleet.Infrastructure.Persistence;

namespace SentinelFleet.Infrastructure.Assets;

public sealed class AssetService(
    SentinelFleetDbContext db,
    IOrganizationContext organizationContext) : IAssetService
{
    // Oslo city center placeholders until live telemetry (Week 3).
    private static readonly (double Lat, double Lng)[] PlaceholderPositions =
    [
        (59.9139, 10.7522),
        (59.9111, 10.7500),
        (59.9165, 10.7580),
        (59.9098, 10.7465),
        (59.9180, 10.7400)
    ];

    public async Task<AssetResult<IReadOnlyList<AssetTypeDto>>> ListAssetTypesAsync(
        CancellationToken cancellationToken = default)
    {
        var types = await db.AssetTypes.AsNoTracking()
            .Where(t => t.OrganizationId == organizationContext.OrganizationId)
            .OrderBy(t => t.Name)
            .Select(t => new AssetTypeDto(t.Id, t.Name, t.Icon, t.Description))
            .ToListAsync(cancellationToken);

        return AssetResult<IReadOnlyList<AssetTypeDto>>.Success(types);
    }

    public async Task<AssetResult<AssetTypeDto>> CreateAssetTypeAsync(
        CreateAssetTypeRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!organizationContext.CanMutate)
        {
            return AssetResult<AssetTypeDto>.Failure(
                new AssetError(AssetErrorCode.Forbidden, "Insufficient permissions."));
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return AssetResult<AssetTypeDto>.Failure(
                new AssetError(AssetErrorCode.Validation, "Name is required."));
        }

        var entity = new AssetType
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationContext.OrganizationId,
            Name = request.Name.Trim(),
            Icon = request.Icon,
            Description = request.Description
        };

        db.AssetTypes.Add(entity);
        await db.SaveChangesAsync(cancellationToken);

        return AssetResult<AssetTypeDto>.Success(
            new AssetTypeDto(entity.Id, entity.Name, entity.Icon, entity.Description));
    }

    public async Task<AssetResult<IReadOnlyList<AssetDto>>> ListAssetsAsync(
        CancellationToken cancellationToken = default)
    {
        var assets = await db.Assets.AsNoTracking()
            .Include(a => a.AssetType)
            .Where(a => a.OrganizationId == organizationContext.OrganizationId)
            .OrderBy(a => a.Name)
            .ToListAsync(cancellationToken);

        return AssetResult<IReadOnlyList<AssetDto>>.Success(assets.Select(ToDto).ToList());
    }

    public async Task<AssetResult<AssetDto>> GetAssetAsync(
        Guid assetId,
        CancellationToken cancellationToken = default)
    {
        var asset = await FindInOrgAsync(assetId, cancellationToken);
        if (asset is null)
        {
            return AssetResult<AssetDto>.Failure(
                new AssetError(AssetErrorCode.NotFound, "Asset not found."));
        }

        return AssetResult<AssetDto>.Success(ToDto(asset));
    }

    public async Task<AssetResult<CreateAssetResponse>> CreateAssetAsync(
        CreateAssetRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!organizationContext.CanMutate)
        {
            return AssetResult<CreateAssetResponse>.Failure(
                new AssetError(AssetErrorCode.Forbidden, "Insufficient permissions."));
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return AssetResult<CreateAssetResponse>.Failure(
                new AssetError(AssetErrorCode.Validation, "Name is required."));
        }

        Guid assetTypeId;
        if (request.AssetTypeId is Guid requestedTypeId)
        {
            var typeExists = await db.AssetTypes.AnyAsync(
                t => t.Id == requestedTypeId && t.OrganizationId == organizationContext.OrganizationId,
                cancellationToken);
            if (!typeExists)
            {
                return AssetResult<CreateAssetResponse>.Failure(
                    new AssetError(AssetErrorCode.Validation, "Asset type not found."));
            }

            assetTypeId = requestedTypeId;
        }
        else
        {
            var defaultType = await db.AssetTypes
                .Where(t => t.OrganizationId == organizationContext.OrganizationId)
                .OrderBy(t => t.Name)
                .FirstOrDefaultAsync(cancellationToken);

            if (defaultType is null)
            {
                return AssetResult<CreateAssetResponse>.Failure(
                    new AssetError(AssetErrorCode.Validation, "No asset type available."));
            }

            assetTypeId = defaultType.Id;
        }

        var now = DateTimeOffset.UtcNow;
        var count = await db.Assets.CountAsync(
            a => a.OrganizationId == organizationContext.OrganizationId,
            cancellationToken);
        var placeholder = PlaceholderPositions[count % PlaceholderPositions.Length];

        var asset = new Asset
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationContext.OrganizationId,
            AssetTypeId = assetTypeId,
            Name = request.Name.Trim(),
            AssetNumber = request.AssetNumber?.Trim(),
            RegistrationNumber = request.RegistrationNumber?.Trim(),
            SerialNumber = request.SerialNumber?.Trim(),
            Manufacturer = request.Manufacturer?.Trim(),
            Model = request.Model?.Trim(),
            Status = AssetStatus.Active,
            Criticality = request.Criticality ?? AssetCriticality.Medium,
            MapLatitude = placeholder.Lat,
            MapLongitude = placeholder.Lng,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.Assets.Add(asset);

        string? apiKey = null;
        if (request.CreateDevice)
        {
            apiKey = GenerateApiKey();
            db.Devices.Add(new Device
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationContext.OrganizationId,
                AssetId = asset.Id,
                ExternalDeviceId = $"dev-{asset.Id:N}"[..20],
                DeviceType = "gps-tracker",
                Status = DeviceStatus.Active,
                ApiKeyHash = DeviceApiKeyHasher.Hash(apiKey),
                CreatedAt = now
            });
        }

        await db.SaveChangesAsync(cancellationToken);

        var created = await db.Assets.AsNoTracking()
            .Include(a => a.AssetType)
            .FirstAsync(a => a.Id == asset.Id, cancellationToken);

        return AssetResult<CreateAssetResponse>.Success(new CreateAssetResponse(ToDto(created), apiKey));
    }

    public async Task<AssetResult<AssetDto>> UpdateAssetAsync(
        Guid assetId,
        UpdateAssetRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!organizationContext.CanMutate)
        {
            return AssetResult<AssetDto>.Failure(
                new AssetError(AssetErrorCode.Forbidden, "Insufficient permissions."));
        }

        var asset = await db.Assets
            .Include(a => a.AssetType)
            .FirstOrDefaultAsync(
                a => a.Id == assetId && a.OrganizationId == organizationContext.OrganizationId,
                cancellationToken);

        if (asset is null)
        {
            return AssetResult<AssetDto>.Failure(
                new AssetError(AssetErrorCode.NotFound, "Asset not found."));
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            asset.Name = request.Name.Trim();
        }

        if (request.AssetTypeId is Guid typeId)
        {
            var typeExists = await db.AssetTypes.AnyAsync(
                t => t.Id == typeId && t.OrganizationId == organizationContext.OrganizationId,
                cancellationToken);
            if (!typeExists)
            {
                return AssetResult<AssetDto>.Failure(
                    new AssetError(AssetErrorCode.Validation, "Asset type not found."));
            }

            asset.AssetTypeId = typeId;
        }

        if (request.AssetNumber is not null) asset.AssetNumber = NullIfWhiteSpace(request.AssetNumber);
        if (request.RegistrationNumber is not null) asset.RegistrationNumber = NullIfWhiteSpace(request.RegistrationNumber);
        if (request.SerialNumber is not null) asset.SerialNumber = NullIfWhiteSpace(request.SerialNumber);
        if (request.Manufacturer is not null) asset.Manufacturer = NullIfWhiteSpace(request.Manufacturer);
        if (request.Model is not null) asset.Model = NullIfWhiteSpace(request.Model);
        if (request.Status is not null) asset.Status = request.Status.Value;
        if (request.Criticality is not null) asset.Criticality = request.Criticality.Value;
        if (request.MapLatitude is not null) asset.MapLatitude = request.MapLatitude;
        if (request.MapLongitude is not null) asset.MapLongitude = request.MapLongitude;

        asset.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        await db.Entry(asset).Reference(a => a.AssetType).LoadAsync(cancellationToken);
        return AssetResult<AssetDto>.Success(ToDto(asset));
    }

    public async Task<AssetResult<AssetStatusDto>> GetCurrentStatusAsync(
        Guid assetId,
        CancellationToken cancellationToken = default)
    {
        var asset = await FindInOrgAsync(assetId, cancellationToken);
        if (asset is null)
        {
            return AssetResult<AssetStatusDto>.Failure(
                new AssetError(AssetErrorCode.NotFound, "Asset not found."));
        }

        return AssetResult<AssetStatusDto>.Success(
            new AssetStatusDto(asset.Id, asset.Status, asset.Criticality, asset.UpdatedAt));
    }

    private async Task<Asset?> FindInOrgAsync(Guid assetId, CancellationToken cancellationToken)
    {
        return await db.Assets.AsNoTracking()
            .Include(a => a.AssetType)
            .FirstOrDefaultAsync(
                a => a.Id == assetId && a.OrganizationId == organizationContext.OrganizationId,
                cancellationToken);
    }

    private static AssetDto ToDto(Asset asset) => new(
        asset.Id,
        asset.AssetTypeId,
        asset.AssetType.Name,
        asset.Name,
        asset.AssetNumber,
        asset.RegistrationNumber,
        asset.SerialNumber,
        asset.Manufacturer,
        asset.Model,
        asset.Status,
        asset.Criticality,
        asset.CurrentUserId,
        asset.MapLatitude,
        asset.MapLongitude,
        asset.CreatedAt,
        asset.UpdatedAt);

    private static string? NullIfWhiteSpace(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string GenerateApiKey()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes);
    }
}
