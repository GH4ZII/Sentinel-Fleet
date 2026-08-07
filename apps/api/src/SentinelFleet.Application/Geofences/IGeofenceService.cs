namespace SentinelFleet.Application.Geofences;

public interface IGeofenceService
{
    Task<GeofenceResult<IReadOnlyList<GeofenceDto>>> ListAsync(
        CancellationToken cancellationToken = default);

    Task<GeofenceResult<GeofenceDto>> GetAsync(
        Guid geofenceId,
        CancellationToken cancellationToken = default);

    Task<GeofenceResult<GeofenceDto>> CreateAsync(
        CreateGeofenceRequest request,
        CancellationToken cancellationToken = default);

    Task<GeofenceResult<GeofenceDto>> UpdateAsync(
        Guid geofenceId,
        UpdateGeofenceRequest request,
        CancellationToken cancellationToken = default);

    Task<GeofenceResult> DeleteAsync(
        Guid geofenceId,
        CancellationToken cancellationToken = default);

    Task<GeofenceResult<IReadOnlyList<AssetGeofenceDto>>> ListLinkedAssetsAsync(
        Guid geofenceId,
        CancellationToken cancellationToken = default);

    Task<GeofenceResult<AssetGeofenceDto>> LinkAssetAsync(
        Guid geofenceId,
        LinkAssetGeofenceRequest request,
        CancellationToken cancellationToken = default);

    Task<GeofenceResult> UnlinkAssetAsync(
        Guid geofenceId,
        Guid assetId,
        CancellationToken cancellationToken = default);
}

public class GeofenceResult
{
    public bool Succeeded { get; init; }

    public GeofenceError? Error { get; init; }

    public static GeofenceResult Success() => new() { Succeeded = true };

    public static GeofenceResult Failure(GeofenceError error) =>
        new() { Succeeded = false, Error = error };
}

public sealed class GeofenceResult<T> : GeofenceResult
{
    public T? Value { get; init; }

    public static GeofenceResult<T> Success(T value) =>
        new() { Succeeded = true, Value = value };

    public static new GeofenceResult<T> Failure(GeofenceError error) =>
        new() { Succeeded = false, Error = error };
}

public sealed record GeofenceError(GeofenceErrorCode Code, string Message);

public enum GeofenceErrorCode
{
    Validation,
    NotFound,
    Forbidden
}
