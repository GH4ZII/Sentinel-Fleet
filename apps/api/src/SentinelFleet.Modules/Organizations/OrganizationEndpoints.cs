using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SentinelFleet.Application.Organizations;

namespace SentinelFleet.Modules.Organizations;

public static class OrganizationEndpoints
{
    public static IEndpointRouteBuilder MapOrganizationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/organizations")
            .WithTags("Organizations")
            .RequireAuthorization();

        group.MapGet("/current", GetCurrentAsync);
        group.MapPatch("/current", UpdateCurrentAsync);
        group.MapGet("/current/members", ListMembersAsync);
        group.MapPatch("/current/members/{memberId:guid}", UpdateMemberAsync);
        group.MapPost("/current/invitations", CreateInvitationAsync);

        var invitations = endpoints.MapGroup("/api/v1/invitations")
            .WithTags("Invitations")
            .AllowAnonymous();

        invitations.MapGet("/{token}", GetInvitationAsync);
        invitations.MapPost("/{token}/accept", AcceptInvitationAsync);

        return endpoints;
    }

    private static async Task<IResult> GetCurrentAsync(
        IOrganizationService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetCurrentAsync(cancellationToken);
        return ToHttp(result);
    }

    private static async Task<IResult> UpdateCurrentAsync(
        UpdateOrganizationRequest request,
        IOrganizationService service,
        CancellationToken cancellationToken)
    {
        var result = await service.UpdateCurrentAsync(request, cancellationToken);
        return ToHttp(result);
    }

    private static async Task<IResult> ListMembersAsync(
        IOrganizationService service,
        CancellationToken cancellationToken)
    {
        var result = await service.ListMembersAsync(cancellationToken);
        return ToHttp(result);
    }

    private static async Task<IResult> UpdateMemberAsync(
        Guid memberId,
        UpdateMemberRequest request,
        IOrganizationService service,
        CancellationToken cancellationToken)
    {
        var result = await service.UpdateMemberAsync(memberId, request, cancellationToken);
        return ToHttp(result);
    }

    private static async Task<IResult> CreateInvitationAsync(
        CreateInvitationRequest request,
        IInvitationService service,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return ToHttp(result, StatusCodes.Status201Created);
    }

    private static async Task<IResult> GetInvitationAsync(
        string token,
        IInvitationService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetAsync(token, cancellationToken);
        return ToHttp(result);
    }

    private static async Task<IResult> AcceptInvitationAsync(
        string token,
        AcceptInvitationRequest request,
        IInvitationService service,
        CancellationToken cancellationToken)
    {
        var result = await service.AcceptAsync(token, request, cancellationToken);
        return ToHttp(result);
    }

    private static IResult ToHttp<T>(OrganizationResult<T> result, int successStatus = StatusCodes.Status200OK)
    {
        if (!result.Succeeded)
        {
            return MapError(result.Error!);
        }

        return successStatus == StatusCodes.Status201Created
            ? Results.Created("/api/v1/organizations/current/invitations", result.Value)
            : Results.Ok(result.Value);
    }

    private static IResult MapError(OrganizationError error) => error.Code switch
    {
        OrganizationErrorCode.Validation => Results.BadRequest(new { error = error.Message }),
        OrganizationErrorCode.NotFound => Results.NotFound(new { error = error.Message }),
        OrganizationErrorCode.Forbidden => Results.Json(new { error = error.Message }, statusCode: StatusCodes.Status403Forbidden),
        OrganizationErrorCode.Conflict => Results.Conflict(new { error = error.Message }),
        _ => Results.Problem(error.Message)
    };
}
