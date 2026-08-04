namespace SentinelFleet.Application.Identity;

public interface IAuthService
{
    Task<AuthResult<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    Task<AuthResult<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task<AuthResult<AuthResponse>> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken = default);

    Task<AuthResult> LogoutAsync(LogoutRequest request, CancellationToken cancellationToken = default);

    Task<AuthResult<UserDto>> GetMeAsync(Guid userId, CancellationToken cancellationToken = default);
}

public class AuthResult
{
    public bool Succeeded { get; init; }

    public AuthError? Error { get; init; }

    public static AuthResult Success() => new() { Succeeded = true };

    public static AuthResult Failure(AuthError error) => new() { Succeeded = false, Error = error };
}

public sealed class AuthResult<T> : AuthResult
{
    public T? Value { get; init; }

    public static AuthResult<T> Success(T value) => new() { Succeeded = true, Value = value };

    public static new AuthResult<T> Failure(AuthError error) => new() { Succeeded = false, Error = error };
}

public sealed record AuthError(AuthErrorCode Code, string Message);

public enum AuthErrorCode
{
    Validation,
    InvalidCredentials,
    Conflict,
    Unauthorized,
    NotFound
}
