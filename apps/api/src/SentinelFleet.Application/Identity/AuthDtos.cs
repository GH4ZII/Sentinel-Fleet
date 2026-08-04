namespace SentinelFleet.Application.Identity;

public sealed record RegisterRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string? OrganizationName = null);

public sealed record LoginRequest(
    string Email,
    string Password);

public sealed record RefreshRequest(string RefreshToken);

public sealed record LogoutRequest(string RefreshToken);

public sealed record UserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    DateTimeOffset? LastLoginAt,
    Guid? OrganizationId = null,
    string? OrganizationRole = null);

public sealed record AuthResponse(
    UserDto User,
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresAt,
    DateTimeOffset RefreshTokenExpiresAt);
