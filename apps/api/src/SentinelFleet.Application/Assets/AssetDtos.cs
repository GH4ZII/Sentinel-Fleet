using SentinelFleet.Domain.Assets;

namespace SentinelFleet.Application.Assets;

public sealed record AssetTypeDto(
    Guid Id,
    string Name,
    string? Icon,
    string? Description);

public sealed record CreateAssetTypeRequest(
    string Name,
    string? Icon,
    string? Description);

public sealed record AssetDto(
    Guid Id,
    Guid AssetTypeId,
    string AssetTypeName,
    string Name,
    string? AssetNumber,
    string? RegistrationNumber,
    string? SerialNumber,
    string? Manufacturer,
    string? Model,
    AssetStatus Status,
    AssetCriticality Criticality,
    Guid? CurrentUserId,
    double? MapLatitude,
    double? MapLongitude,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CreateAssetRequest(
    string Name,
    Guid? AssetTypeId = null,
    string? AssetNumber = null,
    string? RegistrationNumber = null,
    string? SerialNumber = null,
    string? Manufacturer = null,
    string? Model = null,
    AssetCriticality? Criticality = null,
    bool CreateDevice = true);

public sealed record UpdateAssetRequest(
    string? Name,
    Guid? AssetTypeId,
    string? AssetNumber,
    string? RegistrationNumber,
    string? SerialNumber,
    string? Manufacturer,
    string? Model,
    AssetStatus? Status,
    AssetCriticality? Criticality,
    double? MapLatitude,
    double? MapLongitude);

public sealed record AssetStatusDto(
    Guid AssetId,
    AssetStatus Status,
    AssetCriticality Criticality,
    DateTimeOffset UpdatedAt);

public sealed record CreateAssetResponse(
    AssetDto Asset,
    string? DeviceApiKey);
