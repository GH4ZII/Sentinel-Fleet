using Microsoft.EntityFrameworkCore;
using SentinelFleet.Domain.Assets;
using SentinelFleet.Domain.Audit;
using SentinelFleet.Domain.Detections;
using SentinelFleet.Domain.Devices;
using SentinelFleet.Domain.Drivers;
using SentinelFleet.Domain.Geofences;
using SentinelFleet.Domain.Identity;
using SentinelFleet.Domain.Incidents;
using SentinelFleet.Domain.Organizations;
using SentinelFleet.Domain.Rules;
using SentinelFleet.Domain.Telemetry;

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

    public DbSet<TelemetryEvent> TelemetryEvents => Set<TelemetryEvent>();

    public DbSet<Geofence> Geofences => Set<Geofence>();

    public DbSet<AssetGeofence> AssetGeofences => Set<AssetGeofence>();

    public DbSet<AssetPresence> AssetPresences => Set<AssetPresence>();

    public DbSet<DetectionRule> DetectionRules => Set<DetectionRule>();

    public DbSet<Detection> Detections => Set<Detection>();

    public DbSet<DriverAssignment> DriverAssignments => Set<DriverAssignment>();

    public DbSet<WorkShift> WorkShifts => Set<WorkShift>();

    public DbSet<Incident> Incidents => Set<Incident>();

    public DbSet<IncidentTimelineEntry> IncidentTimelineEntries => Set<IncidentTimelineEntry>();

    public DbSet<IncidentEntity> IncidentEntities => Set<IncidentEntity>();

    public DbSet<IncidentComment> IncidentComments => Set<IncidentComment>();

    public DbSet<IncidentAttachment> IncidentAttachments => Set<IncidentAttachment>();

    public DbSet<RiskAssessment> RiskAssessments => Set<RiskAssessment>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("sentinel");
        modelBuilder.HasPostgresExtension("postgis");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SentinelFleetDbContext).Assembly);
    }
}
