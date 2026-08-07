namespace SentinelFleet.Domain.Audit;

public sealed class AuditLog
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid? UserId { get; set; }

    public required string Action { get; set; }

    public required string EntityType { get; set; }

    public Guid EntityId { get; set; }

    public string? OldValues { get; set; }

    public string? NewValues { get; set; }

    public string? IpAddress { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
