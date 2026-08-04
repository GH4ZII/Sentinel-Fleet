using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SentinelFleet.Application.Identity;

namespace SentinelFleet.Modules.Identity;

public static class IdentityEndpoints
{
    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/auth").WithTags("Authentication");

        group.MapPost("/register", RegisterAsync);
        group.MapPost("/login", LoginAsync);
        group.MapPost("/refresh", RefreshAsync);
        group.MapPost("/logout", LogoutAsync);
        group.MapGet("/me", GetMeAsync).RequireAuthorization();

        return endpoints;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest request,
        IAuthService authService,
        CancellationToken cancellationToken)
    {
        var result = await authService.RegisterAsync(request, cancellationToken);
        return ToHttpResult(result, StatusCodes.Status201Created);
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        IAuthService authService,
        CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request, cancellationToken);
        return ToHttpResult(result);
    }

    private static async Task<IResult> RefreshAsync(
        RefreshRequest request,
        IAuthService authService,
        CancellationToken cancellationToken)
    {
        var result = await authService.RefreshAsync(request, cancellationToken);
        return ToHttpResult(result);
    }

    private static async Task<IResult> LogoutAsync(
        LogoutRequest request,
        IAuthService authService,
        CancellationToken cancellationToken)
    {
        var result = await authService.LogoutAsync(request, cancellationToken);
        if (!result.Succeeded)
        {
            return MapError(result.Error!);
        }

        return Results.NoContent();
    }

    private static async Task<IResult> GetMeAsync(
        ClaimsPrincipal principal,
        IAuthService authService,
        CancellationToken cancellationToken)
    {
        var userIdValue = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return Results.Unauthorized();
        }

        var result = await authService.GetMeAsync(userId, cancellationToken);
        return ToHttpResult(result);
    }

    private static IResult ToHttpResult<T>(AuthResult<T> result, int successStatusCode = StatusCodes.Status200OK)
    {
        if (!result.Succeeded)
        {
            return MapError(result.Error!);
        }

        return successStatusCode == StatusCodes.Status201Created
            ? Results.Created("/api/v1/auth/me", result.Value)
            : Results.Ok(result.Value);
    }

    private static IResult MapError(AuthError error) => error.Code switch
    {
        AuthErrorCode.Validation => Results.BadRequest(new { error = error.Message }),
        AuthErrorCode.Conflict => Results.Conflict(new { error = error.Message }),
        AuthErrorCode.InvalidCredentials => Results.Unauthorized(),
        AuthErrorCode.Unauthorized => Results.Unauthorized(),
        AuthErrorCode.NotFound => Results.NotFound(new { error = error.Message }),
        _ => Results.Problem(error.Message)
    };
}
