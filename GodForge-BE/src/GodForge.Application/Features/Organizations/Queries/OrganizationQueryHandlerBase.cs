using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models;
using GodForge.Application.Common.Security;
using GodForge.Domain.Entities.Core;
using GodForge.Domain.Enums;

namespace GodForge.Application.Features.Organizations.Queries;

public abstract class OrganizationQueryHandlerBase
{
    protected readonly IOrganizationRepository _organizations;
    protected readonly IOrganizationMemberRepository _members;

    protected OrganizationQueryHandlerBase(
        IOrganizationRepository organizations,
        IOrganizationMemberRepository members)
    {
        _organizations = organizations;
        _members = members;
    }

    protected async Task<(Organization? Organization, OrganizationMember? Membership, ApplicationError? Error)> GetActiveAccessAsync(
        Guid actorId, Guid organizationId, string permission, CancellationToken cancellationToken)
    {
        var organization = await _organizations.GetByIdAsync(organizationId, cancellationToken);
        var membership = await _members.GetAsync(organizationId, actorId, cancellationToken);
        if (organization is null || organization.Status == OrganizationStatus.Deleted || membership is not { Status: MembershipStatus.Active })
            return (null, null, ApplicationError.NotFound("ORGANIZATION_NOT_FOUND", "Organization was not found."));
        if (!OrganizationRolePermissions.GetPermissionsForRole(membership.Role).Contains(permission))
            return (null, null, ApplicationError.Forbidden("SECURITY_FORBIDDEN", "You do not have permission for this organization operation."));
        if (organization.Status != OrganizationStatus.Active && permission != Permissions.OrganizationsRead)
            return (null, null, ApplicationError.Conflict("ORGANIZATION_NOT_ACTIVE", "Organization is not active."));
        return (organization, membership, null);
    }
}
