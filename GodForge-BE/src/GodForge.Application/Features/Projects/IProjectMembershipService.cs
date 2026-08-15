using GodForge.Application.Common.Models;
using GodForge.Application.Features.Projects.DTOs;

namespace GodForge.Application.Features.Projects;

public interface IProjectMembershipService
{
    Task<Result<ProjectOwnershipTransferDto>> TransferOwnershipAsync(Guid actorId, Guid projectId, Guid newOwnerUserId, string retainCurrentOwnerAs, long version, CancellationToken cancellationToken);
    Task<Result<PagedResult<ProjectMemberDto>>> ListMembersAsync(Guid actorId, Guid projectId, int page, int pageSize, string? role, string? status, string? search, CancellationToken cancellationToken);
    Task<Result<ProjectMemberDto>> AddMemberAsync(Guid actorId, Guid projectId, Guid userId, string role, CancellationToken cancellationToken);
    Task<Result<ProjectMemberDto>> UpdateMemberAsync(Guid actorId, Guid projectId, Guid userId, string role, long version, CancellationToken cancellationToken);
    Task<Result> RemoveMemberAsync(Guid actorId, Guid projectId, Guid userId, CancellationToken cancellationToken);
}
