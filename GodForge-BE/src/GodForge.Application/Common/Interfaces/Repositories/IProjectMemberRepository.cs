using GodForge.Domain.Entities.Core;

namespace GodForge.Application.Common.Interfaces.Repositories;

public interface IProjectMemberRepository
{
    Task<ProjectMember?> GetMembershipAsync(Guid projectId, Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(ProjectMember member, CancellationToken cancellationToken = default);
    Task<int> GetOwnerCountAsync(Guid projectId, CancellationToken cancellationToken = default);
}
