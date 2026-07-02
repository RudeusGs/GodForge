using GodForge.Domain.Entities;
using GodForge.Domain.Entities.Repo;

namespace GodForge.Application.Common.Interfaces.Repositories;

public interface IGitRepositoryRepository
{
    Task<GitRepository?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(GitRepository repository, CancellationToken cancellationToken = default);
}
