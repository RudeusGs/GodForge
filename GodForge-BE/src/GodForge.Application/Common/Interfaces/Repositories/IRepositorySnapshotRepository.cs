using GodForge.Domain.Entities.Repo;

namespace GodForge.Application.Common.Interfaces.Repositories;

public interface IRepositorySnapshotRepository
{
    Task<RepositorySnapshot?> GetByCommitAsync(Guid repositoryId, string commitSha, CancellationToken cancellationToken = default);
    Task AddAsync(RepositorySnapshot snapshot, CancellationToken cancellationToken = default);
}
