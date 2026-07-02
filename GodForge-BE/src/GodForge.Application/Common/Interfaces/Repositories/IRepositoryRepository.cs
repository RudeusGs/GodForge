using GodForge.Domain.Entities;
using GodForge.Domain.Entities.Repo;

namespace GodForge.Application.Common.Interfaces.Repositories;

public interface IRepositoryRepository
{
    Task<Repository?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Repository repository, CancellationToken cancellationToken = default);
}
