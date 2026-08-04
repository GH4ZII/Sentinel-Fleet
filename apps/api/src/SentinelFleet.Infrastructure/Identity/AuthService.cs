using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SentinelFleet.Application.Identity;
using SentinelFleet.Domain.Assets;
using SentinelFleet.Domain.Identity;
using SentinelFleet.Domain.Organizations;
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

        var orgName = string.IsNullOrWhiteSpace(request.OrganizationName)
            ? $"{user.FirstName}'s organization"
            : request.OrganizationName.Trim();

        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = orgName,
            CreatedAt = DateTimeOffset.UtcNow,
            Settings = "{}"
        };

        var membership = new Membership
        {
            Id = Guid.NewGuid(),
            OrganizationId = organization.Id,
            UserId = user.Id,
            Role = OrganizationRole.Owner,
            Status = MembershipStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var defaultAssetType = new AssetType
        {
            Id = Guid.NewGuid(),
            OrganizationId = organization.Id,
            Name = "Kjøretøy",
            Icon = "vehicle",
            Description = "Default vehicle asset type"
        };

        db.Users.Add(user);
        db.Organizations.Add(organization);
        db.Memberships.Add(membership);
        db.AssetTypes.Add(defaultAssetType);

        var response = IssueTokens(user, organization.Id, OrganizationRole.Owner);
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

        var membership = await GetActiveMembershipAsync(user.Id, cancellationToken);
        if (membership is null)
        {
            return AuthResult<AuthResponse>.Failure(
                new AuthError(AuthErrorCode.Unauthorized, "User has no active organization membership."));
        }

        user.LastLoginAt = DateTimeOffset.UtcNow;
        var response = IssueTokens(user, membership.OrganizationId, membership.Role);
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

        var membership = await GetActiveMembershipAsync(existing.UserId, cancellationToken);
        if (membership is null)
        {
            return AuthResult<AuthResponse>.Failure(
                new AuthError(AuthErrorCode.Unauthorized, "User has no active organization membership."));
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

        var (accessToken, accessExpiresAt) = tokenService.CreateAccessToken(
            existing.User,
            membership.OrganizationId,
            membership.Role);

        await db.SaveChangesAsync(cancellationToken);

        return AuthResult<AuthResponse>.Success(new AuthResponse(
            ToDto(existing.User, membership.OrganizationId, membership.Role),
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

        var membership = await GetActiveMembershipAsync(userId, cancellationToken);
        return AuthResult<UserDto>.Success(ToDto(
            user,
            membership?.OrganizationId,
            membership?.Role));
    }

    private async Task<Membership?> GetActiveMembershipAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await db.Memberships
            .AsNoTracking()
            .Where(m => m.UserId == userId && m.Status == MembershipStatus.Active)
            .OrderBy(m => m.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private AuthResponse IssueTokens(User user, Guid organizationId, OrganizationRole role)
    {
        var (accessToken, accessExpiresAt) = tokenService.CreateAccessToken(user, organizationId, role);
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
            ToDto(user, organizationId, role),
            accessToken,
            plaintext,
            accessExpiresAt,
            refreshExpiresAt);
    }

    private static UserDto ToDto(User user, Guid? organizationId, OrganizationRole? role) => new(
        user.Id,
        user.Email,
        user.FirstName,
        user.LastName,
        user.LastLoginAt,
        organizationId,
        role?.ToString());

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
