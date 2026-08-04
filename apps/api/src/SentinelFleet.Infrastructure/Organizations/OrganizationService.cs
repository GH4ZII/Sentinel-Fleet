using Microsoft.EntityFrameworkCore;
using SentinelFleet.Application.Organizations;
using SentinelFleet.Application.Security;
using SentinelFleet.Domain.Organizations;
using SentinelFleet.Infrastructure.Persistence;

namespace SentinelFleet.Infrastructure.Organizations;

public sealed class OrganizationService(
    SentinelFleetDbContext db,
    IOrganizationContext organizationContext) : IOrganizationService
{
    public async Task<OrganizationResult<OrganizationDto>> GetCurrentAsync(
        CancellationToken cancellationToken = default)
    {
        var org = await db.Organizations.AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == organizationContext.OrganizationId, cancellationToken);

        if (org is null)
        {
            return OrganizationResult<OrganizationDto>.Failure(
                new OrganizationError(OrganizationErrorCode.NotFound, "Organization not found."));
        }

        return OrganizationResult<OrganizationDto>.Success(ToDto(org));
    }

    public async Task<OrganizationResult<OrganizationDto>> UpdateCurrentAsync(
        UpdateOrganizationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!organizationContext.CanMutate)
        {
            return OrganizationResult<OrganizationDto>.Failure(
                new OrganizationError(OrganizationErrorCode.Forbidden, "Insufficient permissions."));
        }

        var org = await db.Organizations
            .FirstOrDefaultAsync(o => o.Id == organizationContext.OrganizationId, cancellationToken);

        if (org is null)
        {
            return OrganizationResult<OrganizationDto>.Failure(
                new OrganizationError(OrganizationErrorCode.NotFound, "Organization not found."));
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            org.Name = request.Name.Trim();
        }

        if (request.OrganizationNumber is not null)
        {
            org.OrganizationNumber = string.IsNullOrWhiteSpace(request.OrganizationNumber)
                ? null
                : request.OrganizationNumber.Trim();
        }

        if (request.Settings is not null)
        {
            org.Settings = request.Settings;
        }

        await db.SaveChangesAsync(cancellationToken);
        return OrganizationResult<OrganizationDto>.Success(ToDto(org));
    }

    public async Task<OrganizationResult<IReadOnlyList<MemberDto>>> ListMembersAsync(
        CancellationToken cancellationToken = default)
    {
        var members = await db.Memberships.AsNoTracking()
            .Include(m => m.User)
            .Where(m => m.OrganizationId == organizationContext.OrganizationId)
            .OrderBy(m => m.CreatedAt)
            .Select(m => new MemberDto(
                m.Id,
                m.UserId,
                m.User.Email,
                m.User.FirstName,
                m.User.LastName,
                m.Role,
                m.Status,
                m.CreatedAt))
            .ToListAsync(cancellationToken);

        return OrganizationResult<IReadOnlyList<MemberDto>>.Success(members);
    }

    public async Task<OrganizationResult<MemberDto>> UpdateMemberAsync(
        Guid memberId,
        UpdateMemberRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!organizationContext.IsOwner)
        {
            return OrganizationResult<MemberDto>.Failure(
                new OrganizationError(OrganizationErrorCode.Forbidden, "Only owners can update members."));
        }

        var member = await db.Memberships
            .Include(m => m.User)
            .FirstOrDefaultAsync(
                m => m.Id == memberId && m.OrganizationId == organizationContext.OrganizationId,
                cancellationToken);

        if (member is null)
        {
            return OrganizationResult<MemberDto>.Failure(
                new OrganizationError(OrganizationErrorCode.NotFound, "Member not found."));
        }

        if (member.UserId == organizationContext.UserId &&
            request.Role is not null &&
            request.Role != OrganizationRole.Owner)
        {
            return OrganizationResult<MemberDto>.Failure(
                new OrganizationError(OrganizationErrorCode.Validation, "Cannot demote yourself."));
        }

        if (request.Role is not null)
        {
            member.Role = request.Role.Value;
        }

        if (request.Status is not null)
        {
            member.Status = request.Status.Value;
        }

        await db.SaveChangesAsync(cancellationToken);

        return OrganizationResult<MemberDto>.Success(new MemberDto(
            member.Id,
            member.UserId,
            member.User.Email,
            member.User.FirstName,
            member.User.LastName,
            member.Role,
            member.Status,
            member.CreatedAt));
    }

    private static OrganizationDto ToDto(Organization org) => new(
        org.Id,
        org.Name,
        org.OrganizationNumber,
        org.CreatedAt,
        org.Settings);
}
