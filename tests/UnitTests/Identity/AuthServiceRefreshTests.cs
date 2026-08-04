using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SentinelFleet.Application.Assets;
using SentinelFleet.Application.Identity;
using SentinelFleet.Application.Security;
using SentinelFleet.Domain.Identity;
using SentinelFleet.Domain.Organizations;
using SentinelFleet.Infrastructure.Assets;
using SentinelFleet.Infrastructure.Identity;
using SentinelFleet.Infrastructure.Persistence;

namespace SentinelFleet.UnitTests.Identity;

public class AuthServiceRefreshTests
{
    [Fact]
    public async Task RegisterAsync_CreatesOrganizationAndOwnerMembership()
    {
        await using var db = CreateDbContext();
        var sut = CreateAuthService(db);

        var result = await sut.RegisterAsync(new RegisterRequest(
            "owner@example.com",
            "Password1!",
            "Org",
            "Owner",
            "Acme Fleet"));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value!.User.OrganizationId);
        Assert.Equal(OrganizationRole.Owner.ToString(), result.Value.User.OrganizationRole);

        Assert.Equal(1, await db.Organizations.CountAsync());
        Assert.Equal("Acme Fleet", await db.Organizations.Select(o => o.Name).SingleAsync());
        Assert.Equal(1, await db.Memberships.CountAsync(m => m.Role == OrganizationRole.Owner));
        Assert.Equal(1, await db.AssetTypes.CountAsync(t => t.Name == "Kjøretøy"));
    }

    [Fact]
    public async Task RefreshAsync_RotatesToken_AndRevokesOld()
    {
        await using var db = CreateDbContext();
        var sut = CreateAuthService(db);

        var register = await sut.RegisterAsync(new RegisterRequest(
            "refresh@example.com",
            "Password1!",
            "Refresh",
            "User"));

        Assert.True(register.Succeeded);
        var oldRefresh = register.Value!.RefreshToken;

        var refresh = await sut.RefreshAsync(new RefreshRequest(oldRefresh));
        Assert.True(refresh.Succeeded);
        Assert.NotEqual(oldRefresh, refresh.Value!.RefreshToken);

        var storedTokens = await db.RefreshTokens.AsNoTracking().ToListAsync();
        Assert.Equal(2, storedTokens.Count);

        var revoked = storedTokens.Single(t => t.RevokedAt is not null);
        var active = storedTokens.Single(t => t.RevokedAt is null);
        Assert.Equal(active.Id, revoked.ReplacedByTokenId);

        var reuse = await sut.RefreshAsync(new RefreshRequest(oldRefresh));
        Assert.False(reuse.Succeeded);
        Assert.Equal(AuthErrorCode.Unauthorized, reuse.Error!.Code);
    }

    [Fact]
    public async Task LogoutAsync_RevokesRefreshToken()
    {
        await using var db = CreateDbContext();
        var sut = CreateAuthService(db);

        var login = await sut.RegisterAsync(new RegisterRequest(
            "logout@example.com",
            "Password1!",
            "Log",
            "Out"));

        Assert.True(login.Succeeded);
        var refreshToken = login.Value!.RefreshToken;

        var logout = await sut.LogoutAsync(new LogoutRequest(refreshToken));
        Assert.True(logout.Succeeded);

        var refresh = await sut.RefreshAsync(new RefreshRequest(refreshToken));
        Assert.False(refresh.Succeeded);
    }

    private static AuthService CreateAuthService(SentinelFleetDbContext db) =>
        new(db, CreateTokenService(), new PasswordHasher<User>());

    private static SentinelFleetDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<SentinelFleetDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new SentinelFleetDbContext(options);
    }

    private static JwtTokenService CreateTokenService() =>
        new(Options.Create(new JwtOptions
        {
            Issuer = "sentinel-fleet",
            Audience = "sentinel-fleet",
            SigningKey = "unit_test_signing_key_at_least_32_chars!",
            AccessTokenMinutes = 15,
            RefreshTokenDays = 7
        }));
}

public class AssetTenantIsolationTests
{
    [Fact]
    public async Task GetAssetAsync_ReturnsNotFound_ForOtherOrganization()
    {
        await using var db = CreateDbContext();
        var auth = new AuthService(db, CreateTokenService(), new PasswordHasher<User>());

        var orgA = await auth.RegisterAsync(new RegisterRequest(
            "a@example.com", "Password1!", "A", "User", "Org A"));
        var orgB = await auth.RegisterAsync(new RegisterRequest(
            "b@example.com", "Password1!", "B", "User", "Org B"));

        Assert.True(orgA.Succeeded);
        Assert.True(orgB.Succeeded);

        var orgAId = orgA.Value!.User.OrganizationId!.Value;
        var orgBId = orgB.Value!.User.OrganizationId!.Value;
        var userAId = orgA.Value.User.Id;

        var contextA = new FakeOrganizationContext(userAId, orgAId, OrganizationRole.Owner);
        var assetsA = new AssetService(db, contextA);

        var created = await assetsA.CreateAssetAsync(new CreateAssetRequest("Van 1"));
        Assert.True(created.Succeeded);
        var assetId = created.Value!.Asset.Id;

        var contextB = new FakeOrganizationContext(orgB.Value.User.Id, orgBId, OrganizationRole.Owner);
        var assetsB = new AssetService(db, contextB);

        var leaked = await assetsB.GetAssetAsync(assetId);
        Assert.False(leaked.Succeeded);
        Assert.Equal(AssetErrorCode.NotFound, leaked.Error!.Code);

        var listB = await assetsB.ListAssetsAsync();
        Assert.True(listB.Succeeded);
        Assert.Empty(listB.Value!);
    }

    private static SentinelFleetDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<SentinelFleetDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new SentinelFleetDbContext(options);
    }

    private static JwtTokenService CreateTokenService() =>
        new(Options.Create(new JwtOptions
        {
            Issuer = "sentinel-fleet",
            Audience = "sentinel-fleet",
            SigningKey = "unit_test_signing_key_at_least_32_chars!",
            AccessTokenMinutes = 15,
            RefreshTokenDays = 7
        }));

    private sealed class FakeOrganizationContext(
        Guid userId,
        Guid organizationId,
        OrganizationRole role) : IOrganizationContext
    {
        public Guid UserId { get; } = userId;

        public Guid OrganizationId { get; } = organizationId;

        public OrganizationRole Role { get; } = role;

        public bool CanMutate => Role is OrganizationRole.Owner or OrganizationRole.SecurityManager;

        public bool IsOwner => Role == OrganizationRole.Owner;
    }
}
