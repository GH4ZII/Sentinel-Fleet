using Microsoft.EntityFrameworkCore;
using SentinelFleet.Domain.Identity;

namespace SentinelFleet.Infrastructure.Persistence;

public sealed class SentinelFleetDbContext(DbContextOptions<SentinelFleetDbContext> options)
    : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("sentinel");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SentinelFleetDbContext).Assembly);
    }
}
