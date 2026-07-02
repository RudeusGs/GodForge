namespace GodForge.Application.Common.Security;

public interface IAuthorizationService
{
    Task<bool> HasPermissionAsync(Guid userId, Guid projectId, string permission, CancellationToken cancellationToken = default);
    Task<bool> IsSystemAdminAsync(Guid userId, CancellationToken cancellationToken = default);
}
