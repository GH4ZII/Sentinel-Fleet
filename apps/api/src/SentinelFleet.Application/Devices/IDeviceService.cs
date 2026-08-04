namespace SentinelFleet.Application.Devices;

public interface IDeviceService
{
    Task<DeviceResult<IReadOnlyList<DeviceDto>>> ListAsync(CancellationToken cancellationToken = default);

    Task<DeviceResult<DeviceDto>> GetAsync(Guid deviceId, CancellationToken cancellationToken = default);

    Task<DeviceResult<CreateDeviceResponse>> CreateAsync(
        CreateDeviceRequest request,
        CancellationToken cancellationToken = default);

    Task<DeviceResult<DeviceDto>> UpdateAsync(
        Guid deviceId,
        UpdateDeviceRequest request,
        CancellationToken cancellationToken = default);

    Task<DeviceResult<RotateDeviceKeyResponse>> RotateKeyAsync(
        Guid deviceId,
        CancellationToken cancellationToken = default);
}

public class DeviceResult
{
    public bool Succeeded { get; init; }

    public DeviceError? Error { get; init; }

    public static DeviceResult Success() => new() { Succeeded = true };

    public static DeviceResult Failure(DeviceError error) => new() { Succeeded = false, Error = error };
}

public sealed class DeviceResult<T> : DeviceResult
{
    public T? Value { get; init; }

    public static DeviceResult<T> Success(T value) => new() { Succeeded = true, Value = value };

    public static new DeviceResult<T> Failure(DeviceError error) => new() { Succeeded = false, Error = error };
}

public sealed record DeviceError(DeviceErrorCode Code, string Message);

public enum DeviceErrorCode
{
    Validation,
    NotFound,
    Forbidden
}
