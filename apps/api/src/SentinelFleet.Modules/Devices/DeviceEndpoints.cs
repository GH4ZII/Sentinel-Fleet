using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SentinelFleet.Application.Devices;

namespace SentinelFleet.Modules.Devices;

public static class DeviceEndpoints
{
    public static IEndpointRouteBuilder MapDeviceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/devices")
            .WithTags("Devices")
            .RequireAuthorization();

        group.MapGet("/", ListAsync);
        group.MapPost("/", CreateAsync);
        group.MapGet("/{deviceId:guid}", GetAsync);
        group.MapPatch("/{deviceId:guid}", UpdateAsync);
        group.MapPost("/{deviceId:guid}/rotate-key", RotateKeyAsync);

        return endpoints;
    }

    private static async Task<IResult> ListAsync(IDeviceService service, CancellationToken ct)
    {
        var result = await service.ListAsync(ct);
        return ToHttp(result);
    }

    private static async Task<IResult> CreateAsync(
        CreateDeviceRequest request,
        IDeviceService service,
        CancellationToken ct)
    {
        var result = await service.CreateAsync(request, ct);
        return ToHttp(result, StatusCodes.Status201Created);
    }

    private static async Task<IResult> GetAsync(
        Guid deviceId,
        IDeviceService service,
        CancellationToken ct)
    {
        var result = await service.GetAsync(deviceId, ct);
        return ToHttp(result);
    }

    private static async Task<IResult> UpdateAsync(
        Guid deviceId,
        UpdateDeviceRequest request,
        IDeviceService service,
        CancellationToken ct)
    {
        var result = await service.UpdateAsync(deviceId, request, ct);
        return ToHttp(result);
    }

    private static async Task<IResult> RotateKeyAsync(
        Guid deviceId,
        IDeviceService service,
        CancellationToken ct)
    {
        var result = await service.RotateKeyAsync(deviceId, ct);
        return ToHttp(result);
    }

    private static IResult ToHttp<T>(DeviceResult<T> result, int successStatus = StatusCodes.Status200OK)
    {
        if (!result.Succeeded)
        {
            return result.Error!.Code switch
            {
                DeviceErrorCode.Validation => Results.BadRequest(new { error = result.Error.Message }),
                DeviceErrorCode.NotFound => Results.NotFound(new { error = result.Error.Message }),
                DeviceErrorCode.Forbidden => Results.Json(
                    new { error = result.Error.Message },
                    statusCode: StatusCodes.Status403Forbidden),
                _ => Results.Problem(result.Error.Message)
            };
        }

        return successStatus == StatusCodes.Status201Created
            ? Results.Created("/api/v1/devices", result.Value)
            : Results.Ok(result.Value);
    }
}
