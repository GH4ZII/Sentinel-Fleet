using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SentinelFleet.Application.Incidents;
using SentinelFleet.Domain.Incidents;

namespace SentinelFleet.Modules.Incidents;

public static class IncidentEndpoints
{
    public static IEndpointRouteBuilder MapIncidentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/incidents")
            .WithTags("Incidents")
            .RequireAuthorization();

        group.MapGet("/", ListAsync);
        group.MapGet("/{incidentId:guid}", GetAsync);
        group.MapPatch("/{incidentId:guid}", UpdateAsync);
        group.MapGet("/{incidentId:guid}/timeline", GetTimelineAsync);
        group.MapGet("/{incidentId:guid}/relationships", GetRelationshipsAsync);
        group.MapGet("/{incidentId:guid}/positions", GetPositionsAsync);
        group.MapPost("/{incidentId:guid}/comments", AddCommentAsync);
        group.MapPost("/{incidentId:guid}/attachments", AddAttachmentAsync)
            .DisableAntiforgery();
        group.MapGet("/{incidentId:guid}/attachments/{attachmentId:guid}", DownloadAttachmentAsync);
        group.MapPost("/{incidentId:guid}/assign", AssignAsync);
        group.MapPost("/{incidentId:guid}/resolve", ResolveAsync);
        group.MapGet("/{incidentId:guid}/audit", GetAuditAsync);

        return endpoints;
    }

    private static async Task<IResult> ListAsync(
        IIncidentService service,
        Guid? assetId,
        IncidentStatus? status,
        int? limit,
        CancellationToken ct)
    {
        var result = await service.ListAsync(assetId, status, limit ?? 100, ct);
        return ToHttp(result);
    }

    private static async Task<IResult> GetAsync(
        Guid incidentId,
        IIncidentService service,
        CancellationToken ct)
    {
        var result = await service.GetAsync(incidentId, ct);
        return ToHttp(result);
    }

    private static async Task<IResult> UpdateAsync(
        Guid incidentId,
        UpdateIncidentRequest request,
        IIncidentService service,
        HttpContext http,
        CancellationToken ct)
    {
        var result = await service.UpdateAsync(incidentId, request, http.Connection.RemoteIpAddress?.ToString(), ct);
        return ToHttp(result);
    }

    private static async Task<IResult> GetTimelineAsync(
        Guid incidentId,
        IIncidentService service,
        TimelineEntryType? entryType,
        CancellationToken ct)
    {
        var result = await service.GetTimelineAsync(incidentId, entryType, ct);
        return ToHttp(result);
    }

    private static async Task<IResult> GetRelationshipsAsync(
        Guid incidentId,
        IIncidentService service,
        CancellationToken ct)
    {
        var result = await service.GetRelationshipsAsync(incidentId, ct);
        return ToHttp(result);
    }

    private static async Task<IResult> GetPositionsAsync(
        Guid incidentId,
        IIncidentService service,
        CancellationToken ct)
    {
        var result = await service.GetPositionsAsync(incidentId, ct);
        return ToHttp(result);
    }

    private static async Task<IResult> AddCommentAsync(
        Guid incidentId,
        AddIncidentCommentRequest request,
        IIncidentService service,
        HttpContext http,
        CancellationToken ct)
    {
        var result = await service.AddCommentAsync(
            incidentId,
            request,
            http.Connection.RemoteIpAddress?.ToString(),
            ct);
        return ToHttp(result, StatusCodes.Status201Created);
    }

    private static async Task<IResult> AddAttachmentAsync(
        Guid incidentId,
        IFormFile file,
        IIncidentService service,
        HttpContext http,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return Results.BadRequest(new { error = "File is required." });
        }

        await using var stream = file.OpenReadStream();
        var result = await service.AddAttachmentAsync(
            incidentId,
            file.FileName,
            file.ContentType,
            stream,
            file.Length,
            http.Connection.RemoteIpAddress?.ToString(),
            ct);
        return ToHttp(result, StatusCodes.Status201Created);
    }

    private static async Task<IResult> DownloadAttachmentAsync(
        Guid incidentId,
        Guid attachmentId,
        IIncidentService service,
        CancellationToken ct)
    {
        var result = await service.GetAttachmentAsync(incidentId, attachmentId, ct);
        if (!result.Succeeded)
        {
            return ToHttp(result);
        }

        var (attachment, content) = result.Value!;
        return Results.File(content, attachment.ContentType, attachment.Name);
    }

    private static async Task<IResult> AssignAsync(
        Guid incidentId,
        AssignIncidentRequest request,
        IIncidentService service,
        HttpContext http,
        CancellationToken ct)
    {
        var result = await service.AssignAsync(
            incidentId,
            request,
            http.Connection.RemoteIpAddress?.ToString(),
            ct);
        return ToHttp(result);
    }

    private static async Task<IResult> ResolveAsync(
        Guid incidentId,
        ResolveIncidentRequest? request,
        IIncidentService service,
        HttpContext http,
        CancellationToken ct)
    {
        var result = await service.ResolveAsync(
            incidentId,
            request ?? new ResolveIncidentRequest(null),
            http.Connection.RemoteIpAddress?.ToString(),
            ct);
        return ToHttp(result);
    }

    private static async Task<IResult> GetAuditAsync(
        Guid incidentId,
        IIncidentService service,
        CancellationToken ct)
    {
        var result = await service.GetAuditAsync(incidentId, ct);
        return ToHttp(result);
    }

    private static IResult ToHttp<T>(IncidentResult<T> result, int successStatus = StatusCodes.Status200OK)
    {
        if (!result.Succeeded)
        {
            return result.Error!.Code switch
            {
                IncidentErrorCode.Validation => Results.BadRequest(new { error = result.Error.Message }),
                IncidentErrorCode.NotFound => Results.NotFound(new { error = result.Error.Message }),
                IncidentErrorCode.Forbidden => Results.Json(
                    new { error = result.Error.Message },
                    statusCode: StatusCodes.Status403Forbidden),
                _ => Results.Problem(result.Error.Message)
            };
        }

        return successStatus == StatusCodes.Status201Created
            ? Results.Created($"/api/v1/incidents", result.Value)
            : Results.Ok(result.Value);
    }
}
