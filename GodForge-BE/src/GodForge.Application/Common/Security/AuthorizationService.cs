using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Domain.Enums;

namespace GodForge.Application.Common.Security;

public sealed class AuthorizationService : IAuthorizationService
{
    private static readonly HashSet<string> InternalProjectReadPermissions = new(StringComparer.Ordinal)
    {
        Permissions.ProjectsRead,
        Permissions.RepositoryRead,
        Permissions.RevisionsRead,
        Permissions.AnalysisRead,
        Permissions.JobsRead,
        Permissions.ActivityRead
    };

    private readonly IProjectMemberRepository _projectMembers;
    private readonly IUserRepository _users;
    private readonly IProjectRepository _projects;

    public AuthorizationService(
        IProjectMemberRepository projectMembers,
        IUserRepository users,
        IProjectRepository projects)
    {
        _projectMembers = projectMembers;
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

        if (user.SystemRole == SystemRole.SystemAdmin)
        {
            return true;
        }

        var membership = await _projectMembers.GetMembershipAsync(projectId, userId, cancellationToken);
        if (membership is not null)
        {
            return RolePermissions.GetPermissionsForRole(membership.Role).Contains(permission);
        }

        if (!InternalProjectReadPermissions.Contains(permission))
        {
            return false;
        }

        var project = await _projects.GetByIdAsync(projectId, cancellationToken);
        return project is
        {
            Visibility: ProjectVisibility.Internal,
            Status: not ProjectStatus.Deleted,
            DeletedAt: null
        };
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
