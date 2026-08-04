using Microsoft.EntityFrameworkCore;

namespace SentinelFleet.Infrastructure.Persistence;

public sealed class SentinelFleetDbContext(DbContextOptions<SentinelFleetDbContext> options)
    : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("sentinel");
    }
}
