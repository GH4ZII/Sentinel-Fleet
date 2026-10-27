using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SentinelFleet.Application.Identity;
using SentinelFleet.Application.Organizations;
using SentinelFleet.Application.Security;
using SentinelFleet.Domain.Identity;
using SentinelFleet.Domain.Organizations;
using SentinelFleet.Infrastructure.Persistence;

namespace SentinelFleet.Infrastructure.Organizations;

public sealed class InvitationService(
    SentinelFleetDbContext db,
    IOrganizationContext organizationContext,
    IJwtTokenService tokenService,
    PasswordHasher<User> passwordHasher) : IInvitationService
{
    private static readonly TimeSpan InvitationLifetime = TimeSpan.FromDays(7);

    public async Task<OrganizationResult<InvitationCreatedDto>> CreateAsync(
        CreateInvitationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!organizationContext.IsOwner)
        {
            return OrganizationResult<InvitationCreatedDto>.Failure(
                new OrganizationError(OrganizationErrorCode.Forbidden, "Only owners can invite users."));
        }

        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
        {
            return OrganizationResult<InvitationCreatedDto>.Failure(
                new OrganizationError(OrganizationErrorCode.Validation, "A valid email is required."));
        }

        if (request.Role == OrganizationRole.Owner)
        {
            return OrganizationResult<InvitationCreatedDto>.Failure(
                new OrganizationError(OrganizationErrorCode.Validation, "Cannot invite as Owner."));
        }

        var email = NormalizeEmail(request.Email);

        var userExists = await db.Users.AnyAsync(u => u.Email == email, cancellationToken);
        if (userExists)
        {
            return OrganizationResult<InvitationCreatedDto>.Failure(
                new OrganizationError(OrganizationErrorCode.Conflict, "A user with this email already exists."));
        }

        var pendingExists = await db.Invitations.AnyAsync(
            i => i.OrganizationId == organizationContext.OrganizationId
                 && i.Email == email
                 && i.AcceptedAt == null
                 && i.ExpiresAt > DateTimeOffset.UtcNow,
            cancellationToken);

        if (pendingExists)
        {
            return OrganizationResult<InvitationCreatedDto>.Failure(
                new OrganizationError(
                    OrganizationErrorCode.Conflict,
                    "An active invitation already exists for this email."));
        }

        var token = GenerateToken();
        var invitation = new Invitation
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationContext.OrganizationId,
            Email = email,
            Role = request.Role,
            Token = token,
            ExpiresAt = DateTimeOffset.UtcNow.Add(InvitationLifetime),
            CreatedByUserId = organizationContext.UserId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.Invitations.Add(invitation);
        await db.SaveChangesAsync(cancellationToken);

        return OrganizationResult<InvitationCreatedDto>.Success(new InvitationCreatedDto(
            invitation.Id,
            invitation.Email,
            invitation.Role,
            invitation.Token,
            invitation.ExpiresAt,
            $"/accept-invitation?token={Uri.EscapeDataString(token)}"));
    }

    public async Task<OrganizationResult<InvitationInfoDto>> GetAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return OrganizationResult<InvitationInfoDto>.Failure(
                new OrganizationError(OrganizationErrorCode.Validation, "Token is required."));
        }

        var invitation = await db.Invitations.AsNoTracking()
            .Include(i => i.Organization)
            .FirstOrDefaultAsync(i => i.Token == token.Trim(), cancellationToken);

        if (invitation is null)
        {
            return OrganizationResult<InvitationInfoDto>.Failure(
                new OrganizationError(OrganizationErrorCode.NotFound, "Invitation not found."));
        }

        if (invitation.AcceptedAt is not null)
        {
            return OrganizationResult<InvitationInfoDto>.Failure(
                new OrganizationError(OrganizationErrorCode.Conflict, "Invitation has already been accepted."));
        }

        if (invitation.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            return OrganizationResult<InvitationInfoDto>.Failure(
                new OrganizationError(OrganizationErrorCode.Validation, "Invitation has expired."));
        }

        return OrganizationResult<InvitationInfoDto>.Success(new InvitationInfoDto(
            MaskEmail(invitation.Email),
            invitation.Organization.Name,
            invitation.Role,
            invitation.ExpiresAt));
    }

    public async Task<OrganizationResult<AuthResponse>> AcceptAsync(
        string token,
        AcceptInvitationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return OrganizationResult<AuthResponse>.Failure(
                new OrganizationError(OrganizationErrorCode.Validation, "Token is required."));
        }

        if (string.IsNullOrWhiteSpace(request.FirstName) ||
            string.IsNullOrWhiteSpace(request.LastName) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return OrganizationResult<AuthResponse>.Failure(
                new OrganizationError(
                    OrganizationErrorCode.Validation,
                    "First name, last name, and password are required."));
        }

        if (request.Password.Length < 8)
        {
            return OrganizationResult<AuthResponse>.Failure(
                new OrganizationError(
                    OrganizationErrorCode.Validation,
                    "Password must be at least 8 characters."));
        }

        var invitation = await db.Invitations
            .Include(i => i.Organization)
            .FirstOrDefaultAsync(i => i.Token == token.Trim(), cancellationToken);

        if (invitation is null)
        {
            return OrganizationResult<AuthResponse>.Failure(
                new OrganizationError(OrganizationErrorCode.NotFound, "Invitation not found."));
        }

        if (invitation.AcceptedAt is not null)
        {
            return OrganizationResult<AuthResponse>.Failure(
                new OrganizationError(OrganizationErrorCode.Conflict, "Invitation has already been accepted."));
        }

        if (invitation.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            return OrganizationResult<AuthResponse>.Failure(
                new OrganizationError(OrganizationErrorCode.Validation, "Invitation has expired."));
        }

        var email = invitation.Email;
        var exists = await db.Users.AnyAsync(u => u.Email == email, cancellationToken);
        if (exists)
        {
            return OrganizationResult<AuthResponse>.Failure(
                new OrganizationError(OrganizationErrorCode.Conflict, "A user with this email already exists."));
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            PasswordHash = string.Empty,
            CreatedAt = DateTimeOffset.UtcNow,
            LastLoginAt = DateTimeOffset.UtcNow
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        var membership = new Membership
        {
            Id = Guid.NewGuid(),
            OrganizationId = invitation.OrganizationId,
            UserId = user.Id,
            Role = invitation.Role,
            Status = MembershipStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };

        invitation.AcceptedAt = DateTimeOffset.UtcNow;

        var (accessToken, accessExpiresAt) = tokenService.CreateAccessToken(
            user,
            invitation.OrganizationId,
            invitation.Role);
        var (refreshPlaintext, refreshHash, refreshExpiresAt) = tokenService.CreateRefreshToken();

        db.Users.Add(user);
        db.Memberships.Add(membership);
        db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = refreshHash,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = refreshExpiresAt
        });

        await db.SaveChangesAsync(cancellationToken);

        var response = new AuthResponse(
            new UserDto(
                user.Id,
                user.Email,
                user.FirstName,
                user.LastName,
                user.LastLoginAt,
                invitation.OrganizationId,
                invitation.Role.ToString()),
            accessToken,
            refreshPlaintext,
            accessExpiresAt,
            refreshExpiresAt);

        return OrganizationResult<AuthResponse>.Success(response);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static string MaskEmail(string email)
    {
        var at = email.IndexOf('@');
        if (at <= 0)
        {
            return "***";
        }

        var local = email[..at];
        var domain = email[(at + 1)..];
        var visible = local.Length <= 2 ? local[..1] : local[..2];
        return $"{visible}***@{domain}";
    }
}
