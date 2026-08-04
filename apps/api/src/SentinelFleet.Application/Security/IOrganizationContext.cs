using SentinelFleet.Domain.Organizations;

namespace SentinelFleet.Application.Security;

public interface IOrganizationContext
{
    Guid UserId { get; }

    Guid OrganizationId { get; }

    OrganizationRole Role { get; }

    bool CanMutate { get; }

    bool IsOwner { get; }
}
