using Microsoft.EntityFrameworkCore;
using SentinelFleet.Domain.Assets;
using SentinelFleet.Domain.Devices;
using SentinelFleet.Domain.Identity;
using SentinelFleet.Domain.Organizations;

namespace SentinelFleet.Infrastructure.Persistence;

public sealed class SentinelFleetDbContext(DbContextOptions<SentinelFleetDbContext> options)
    : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Organization> Organizations => Set<Organization>();

    public DbSet<Membership> Memberships => Set<Membership>();

    public DbSet<AssetType> AssetTypes => Set<AssetType>();

    public DbSet<Asset> Assets => Set<Asset>();

    public DbSet<Device> Devices => Set<Device>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("sentinel");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SentinelFleetDbContext).Assembly);
    }
}
