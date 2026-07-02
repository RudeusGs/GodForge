using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;

namespace GodForge.Application.Common.Security;

public class AuthorizationService : IAuthorizationService
{
    private readonly IProjectMemberRepository _projectMembers;
    private readonly IUserRepository _users;

    public AuthorizationService(IProjectMemberRepository projectMembers, IUserRepository users)
    {
        _projectMembers = projectMembers;
        _users = users;
    }

    public async Task<bool> HasPermissionAsync(Guid userId, Guid projectId, string permission, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(userId, cancellationToken);
        if (user == null || user.Status != GodForge.Domain.Enums.UserStatus.Active)
            return false;

        if (user.SystemRole == GodForge.Domain.Enums.SystemRole.SystemAdmin)
            return true;

        var membership = await _projectMembers.GetMembershipAsync(projectId, userId, cancellationToken);
        if (membership == null)
        {
            // If the user isn't a member, check if the project is public/internal?
            // For MVP simplicity, requiring membership for all permissions except maybe read on public, 
            // but for simplicity let's stick to membership based here.
            return false; 
        }

        var permissions = RolePermissions.GetPermissionsForRole(membership.Role);
        return permissions.Contains(permission);
    }

    public async Task<bool> IsSystemAdminAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(userId, cancellationToken);
        return user?.SystemRole == GodForge.Domain.Enums.SystemRole.SystemAdmin;
    }
}
