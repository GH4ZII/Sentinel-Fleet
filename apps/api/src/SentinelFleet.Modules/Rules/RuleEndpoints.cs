using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SentinelFleet.Application.Rules;

namespace SentinelFleet.Modules.Rules;

public static class RuleEndpoints
{
    public static IEndpointRouteBuilder MapRuleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/rules")
            .WithTags("Rules")
            .RequireAuthorization();

        group.MapGet("/", ListAsync);
        group.MapPost("/", CreateAsync);
        group.MapGet("/{ruleId:guid}", GetAsync);
        group.MapPatch("/{ruleId:guid}", UpdateAsync);
        group.MapPost("/{ruleId:guid}/enable", EnableAsync);
        group.MapPost("/{ruleId:guid}/disable", DisableAsync);

        return endpoints;
    }

    private static async Task<IResult> ListAsync(IDetectionRuleService service, CancellationToken ct)
    {
        var result = await service.ListAsync(ct);
        return ToHttp(result);
    }

    private static async Task<IResult> CreateAsync(
        CreateDetectionRuleRequest request,
        IDetectionRuleService service,
        CancellationToken ct)
    {
        var result = await service.CreateAsync(request, ct);
        return ToHttp(result, StatusCodes.Status201Created);
    }

    private static async Task<IResult> GetAsync(
        Guid ruleId,
        IDetectionRuleService service,
        CancellationToken ct)
    {
        var result = await service.GetAsync(ruleId, ct);
        return ToHttp(result);
    }

    private static async Task<IResult> UpdateAsync(
        Guid ruleId,
        UpdateDetectionRuleRequest request,
        IDetectionRuleService service,
        CancellationToken ct)
    {
        var result = await service.UpdateAsync(ruleId, request, ct);
        return ToHttp(result);
    }

    private static async Task<IResult> EnableAsync(
        Guid ruleId,
        IDetectionRuleService service,
        CancellationToken ct)
    {
        var result = await service.EnableAsync(ruleId, ct);
        return ToHttp(result);
    }

    private static async Task<IResult> DisableAsync(
        Guid ruleId,
        IDetectionRuleService service,
        CancellationToken ct)
    {
        var result = await service.DisableAsync(ruleId, ct);
        return ToHttp(result);
    }

    private static IResult ToHttp<T>(RuleResult<T> result, int successStatus = StatusCodes.Status200OK)
    {
        if (!result.Succeeded)
        {
            return result.Error!.Code switch
            {
                RuleErrorCode.Validation => Results.BadRequest(new { error = result.Error.Message }),
                RuleErrorCode.NotFound => Results.NotFound(new { error = result.Error.Message }),
                RuleErrorCode.Forbidden => Results.Json(
                    new { error = result.Error.Message },
                    statusCode: StatusCodes.Status403Forbidden),
                _ => Results.Problem(result.Error.Message)
            };
        }

        return successStatus == StatusCodes.Status201Created
            ? Results.Created($"/api/v1/rules", result.Value)
            : Results.Ok(result.Value);
    }
}
