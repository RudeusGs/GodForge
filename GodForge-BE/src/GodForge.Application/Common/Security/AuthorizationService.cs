using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Domain.Enums;

namespace GodForge.Application.Common.Security;

public sealed class AuthorizationService : IAuthorizationService
{
    private readonly IProjectMemberRepository _projectMembers;
    private readonly IOrganizationMemberRepository _organizationMembers;
    private readonly IUserRepository _users;
    private readonly IProjectRepository _projects;

    public AuthorizationService(
        IProjectMemberRepository projectMembers,
        IOrganizationMemberRepository organizationMembers,
        IUserRepository users,
        IProjectRepository projects)
    {
        _projectMembers = projectMembers;
        _organizationMembers = organizationMembers;
        _users = users;
        _projects = projects;
    }

    public async Task<bool> HasPermissionAsync(
        Guid userId,
        Guid projectId,
        string permission,
        CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(userId, cancellationToken);
        if (user is null || user.Status != UserStatus.Active || user.DeletedAt is not null)
        {
            return false;
        }

        var project = await _projects.GetByIdAsync(projectId, cancellationToken);
        if (project is null || project.Status is ProjectStatus.Deleted or ProjectStatus.Deleting || project.DeletedAt is not null)
            return false;

        if (!await _organizationMembers.IsActiveAsync(project.OrganizationId, userId, cancellationToken))
            return false;

        var membership = await _projectMembers.GetMembershipAsync(projectId, userId, cancellationToken);
        return membership is not null &&
            membership.OrganizationId == project.OrganizationId &&
            RolePermissions.GetPermissionsForRole(membership.Role).Contains(permission);
    }

    public async Task<bool> IsSystemAdminAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(userId, cancellationToken);
        return user is
        {
            SystemRole: SystemRole.SystemAdmin,
            Status: UserStatus.Active,
            DeletedAt: null
        };
    }
}
