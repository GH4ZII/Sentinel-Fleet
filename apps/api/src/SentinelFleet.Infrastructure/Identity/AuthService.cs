using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SentinelFleet.Application.Identity;
using SentinelFleet.Domain.Identity;
using SentinelFleet.Infrastructure.Persistence;

namespace SentinelFleet.Infrastructure.Identity;

public sealed class AuthService(
    SentinelFleetDbContext db,
    IJwtTokenService tokenService,
    PasswordHasher<User> passwordHasher) : IAuthService
{
    public async Task<AuthResult<AuthResponse>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationError = ValidateRegister(request);
        if (validationError is not null)
        {
            return AuthResult<AuthResponse>.Failure(validationError);
        }

        var email = NormalizeEmail(request.Email);
        var exists = await db.Users.AnyAsync(u => u.Email == email, cancellationToken);
        if (exists)
        {
            return AuthResult<AuthResponse>.Failure(
                new AuthError(AuthErrorCode.Conflict, "A user with this email already exists."));
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            PasswordHash = string.Empty,
            CreatedAt = DateTimeOffset.UtcNow
        };

        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        db.Users.Add(user);
        var response = IssueTokens(user);
        await db.SaveChangesAsync(cancellationToken);

        return AuthResult<AuthResponse>.Success(response);
    }

    public async Task<AuthResult<AuthResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return AuthResult<AuthResponse>.Failure(
                new AuthError(AuthErrorCode.Validation, "Email and password are required."));
        }

        var email = NormalizeEmail(request.Email);
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
        if (user is null)
        {
            return AuthResult<AuthResponse>.Failure(
                new AuthError(AuthErrorCode.InvalidCredentials, "Invalid email or password."));
        }

        var verify = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verify == PasswordVerificationResult.Failed)
        {
            return AuthResult<AuthResponse>.Failure(
                new AuthError(AuthErrorCode.InvalidCredentials, "Invalid email or password."));
        }

        user.LastLoginAt = DateTimeOffset.UtcNow;
        var response = IssueTokens(user);
        await db.SaveChangesAsync(cancellationToken);

        return AuthResult<AuthResponse>.Success(response);
    }

    public async Task<AuthResult<AuthResponse>> RefreshAsync(
        RefreshRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return AuthResult<AuthResponse>.Failure(
                new AuthError(AuthErrorCode.Validation, "Refresh token is required."));
        }

        var hash = tokenService.HashRefreshToken(request.RefreshToken);
        var existing = await db.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        if (existing is null || !existing.IsActive)
        {
            return AuthResult<AuthResponse>.Failure(
                new AuthError(AuthErrorCode.Unauthorized, "Invalid or expired refresh token."));
        }

        var (plaintext, tokenHash, expiresAt) = tokenService.CreateRefreshToken();
        var replacement = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = existing.UserId,
            TokenHash = tokenHash,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = expiresAt
        };

        existing.RevokedAt = DateTimeOffset.UtcNow;
        existing.ReplacedByTokenId = replacement.Id;

        db.RefreshTokens.Add(replacement);

        var (accessToken, accessExpiresAt) = tokenService.CreateAccessToken(existing.User);
        await db.SaveChangesAsync(cancellationToken);

        return AuthResult<AuthResponse>.Success(new AuthResponse(
            ToDto(existing.User),
            accessToken,
            plaintext,
            accessExpiresAt,
            expiresAt));
    }

    public async Task<AuthResult> LogoutAsync(
        LogoutRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return AuthResult.Failure(
                new AuthError(AuthErrorCode.Validation, "Refresh token is required."));
        }

        var hash = tokenService.HashRefreshToken(request.RefreshToken);
        var existing = await db.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        if (existing is not null && existing.RevokedAt is null)
        {
            existing.RevokedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }

        return AuthResult.Success();
    }

    public async Task<AuthResult<UserDto>> GetMeAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
        {
            return AuthResult<UserDto>.Failure(
                new AuthError(AuthErrorCode.NotFound, "User not found."));
        }

        return AuthResult<UserDto>.Success(ToDto(user));
    }

    private AuthResponse IssueTokens(User user)
    {
        var (accessToken, accessExpiresAt) = tokenService.CreateAccessToken(user);
        var (plaintext, tokenHash, refreshExpiresAt) = tokenService.CreateRefreshToken();

        db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = tokenHash,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = refreshExpiresAt
        });

        return new AuthResponse(
            ToDto(user),
            accessToken,
            plaintext,
            accessExpiresAt,
            refreshExpiresAt);
    }

    private static UserDto ToDto(User user) => new(
        user.Id,
        user.Email,
        user.FirstName,
        user.LastName,
        user.LastLoginAt);

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static AuthError? ValidateRegister(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password) ||
            string.IsNullOrWhiteSpace(request.FirstName) ||
            string.IsNullOrWhiteSpace(request.LastName))
        {
            return new AuthError(AuthErrorCode.Validation, "Email, password, first name, and last name are required.");
        }

        if (request.Password.Length < 8)
        {
            return new AuthError(AuthErrorCode.Validation, "Password must be at least 8 characters.");
        }

        if (!request.Email.Contains('@'))
        {
            return new AuthError(AuthErrorCode.Validation, "Email is invalid.");
        }

        return null;
    }
}
