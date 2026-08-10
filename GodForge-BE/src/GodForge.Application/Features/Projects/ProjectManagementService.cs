using GodForge.Application.Common.Models;
using GodForge.Application.Features.Projects.DTOs;

namespace GodForge.Application.Features.Projects;

/// <summary>
/// Stable facade used by project command/query handlers. Project lifecycle/settings and membership/ownership
/// operations are implemented by separate cohesive services.
/// </summary>
public sealed class ProjectManagementService : IProjectManagementService
{
    private readonly ProjectLifecycleService _lifecycle;
    private readonly ProjectMembershipService _membership;

    public ProjectManagementService(
        ProjectLifecycleService lifecycle,
        ProjectMembershipService membership)
    {
        _lifecycle = lifecycle;
        _membership = membership;
    }

    public Task<Result<PagedResult<ProjectDto>>> ListAsync(Guid actorId, int page, int pageSize, Guid? organizationId, string? status, string? search, CancellationToken cancellationToken)
        => _lifecycle.ListAsync(actorId, page, pageSize, organizationId, status, search, cancellationToken);

    public Task<Result<PagedResult<ProjectAdministrationDto>>> ListForOrganizationAsync(Guid actorId, Guid organizationId, int page, int pageSize, CancellationToken cancellationToken)
        => _lifecycle.ListForOrganizationAsync(actorId, organizationId, page, pageSize, cancellationToken);

    public Task<Result<ProjectDto>> CreateAsync(Guid actorId, Guid organizationId, string name, string slug, string? description, string visibility, string? idempotencyKey, CancellationToken cancellationToken)
        => _lifecycle.CreateAsync(actorId, organizationId, name, slug, description, visibility, idempotencyKey, cancellationToken);

    public Task<Result<ProjectDto>> GetAsync(Guid actorId, Guid projectId, CancellationToken cancellationToken)
        => _lifecycle.GetAsync(actorId, projectId, cancellationToken);

    public Task<Result<ProjectDto>> UpdateAsync(Guid actorId, Guid projectId, string name, string slug, string? description, string visibility, long version, CancellationToken cancellationToken)
        => _lifecycle.UpdateAsync(actorId, projectId, name, slug, description, visibility, version, cancellationToken);

    public Task<Result<ProjectDeletionAcceptedDto>> RequestDeletionAsync(Guid actorId, Guid projectId, long version, string confirmationSlug, CancellationToken cancellationToken)
        => _lifecycle.RequestDeletionAsync(actorId, projectId, version, confirmationSlug, cancellationToken);

    public Task<Result<ProjectDto>> RestoreAsync(Guid actorId, Guid projectId, long version, CancellationToken cancellationToken)
        => _lifecycle.RestoreAsync(actorId, projectId, version, cancellationToken);

    public Task<Result<ProjectOwnershipTransferDto>> TransferOwnershipAsync(Guid actorId, Guid projectId, Guid newOwnerUserId, string retainCurrentOwnerAs, long version, CancellationToken cancellationToken)
        => _membership.TransferOwnershipAsync(actorId, projectId, newOwnerUserId, retainCurrentOwnerAs, version, cancellationToken);

    public Task<Result<PagedResult<ProjectMemberDto>>> ListMembersAsync(Guid actorId, Guid projectId, int page, int pageSize, string? role, string? status, string? search, CancellationToken cancellationToken)
        => _membership.ListMembersAsync(actorId, projectId, page, pageSize, role, status, search, cancellationToken);

    public Task<Result<ProjectMemberDto>> AddMemberAsync(Guid actorId, Guid projectId, Guid userId, string role, CancellationToken cancellationToken)
        => _membership.AddMemberAsync(actorId, projectId, userId, role, cancellationToken);

    public Task<Result<ProjectMemberDto>> UpdateMemberAsync(Guid actorId, Guid projectId, Guid userId, string role, long version, CancellationToken cancellationToken)
        => _membership.UpdateMemberAsync(actorId, projectId, userId, role, version, cancellationToken);

    public Task<Result> RemoveMemberAsync(Guid actorId, Guid projectId, Guid userId, CancellationToken cancellationToken)
        => _membership.RemoveMemberAsync(actorId, projectId, userId, cancellationToken);

    public Task<Result<ProjectSettingsDto>> GetSettingsAsync(Guid actorId, Guid projectId, CancellationToken cancellationToken)
        => _lifecycle.GetSettingsAsync(actorId, projectId, cancellationToken);

    public Task<Result<ProjectSettingsDto>> UpdateSettingsAsync(Guid actorId, Guid projectId, string analysisProfileKey, bool aiAdvisoryEnabled, string defaultAssetVisibility, int notificationPolicyVersion, long version, CancellationToken cancellationToken)
        => _lifecycle.UpdateSettingsAsync(actorId, projectId, analysisProfileKey, aiAdvisoryEnabled, defaultAssetVisibility, notificationPolicyVersion, version, cancellationToken);
}
