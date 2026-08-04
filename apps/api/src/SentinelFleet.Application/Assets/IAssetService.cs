namespace SentinelFleet.Application.Assets;

public interface IAssetService
{
    Task<AssetResult<IReadOnlyList<AssetTypeDto>>> ListAssetTypesAsync(
        CancellationToken cancellationToken = default);

    Task<AssetResult<AssetTypeDto>> CreateAssetTypeAsync(
        CreateAssetTypeRequest request,
        CancellationToken cancellationToken = default);

    Task<AssetResult<IReadOnlyList<AssetDto>>> ListAssetsAsync(
        CancellationToken cancellationToken = default);

    Task<AssetResult<AssetDto>> GetAssetAsync(
        Guid assetId,
        CancellationToken cancellationToken = default);

    Task<AssetResult<CreateAssetResponse>> CreateAssetAsync(
        CreateAssetRequest request,
        CancellationToken cancellationToken = default);

    Task<AssetResult<AssetDto>> UpdateAssetAsync(
        Guid assetId,
        UpdateAssetRequest request,
        CancellationToken cancellationToken = default);

    Task<AssetResult<AssetStatusDto>> GetCurrentStatusAsync(
        Guid assetId,
        CancellationToken cancellationToken = default);
}

public class AssetResult
{
    public bool Succeeded { get; init; }

    public AssetError? Error { get; init; }

    public static AssetResult Success() => new() { Succeeded = true };

    public static AssetResult Failure(AssetError error) => new() { Succeeded = false, Error = error };
}

public sealed class AssetResult<T> : AssetResult
{
    public T? Value { get; init; }

    public static AssetResult<T> Success(T value) => new() { Succeeded = true, Value = value };

    public static new AssetResult<T> Failure(AssetError error) => new() { Succeeded = false, Error = error };
}

public sealed record AssetError(AssetErrorCode Code, string Message);

public enum AssetErrorCode
{
    Validation,
    NotFound,
    Forbidden
}
