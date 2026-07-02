using GodForge.Domain.Entities;
using GodForge.Domain.Entities.Core;
using GodForge.Application.Common.Models;

namespace GodForge.Application.Common.Interfaces.Repositories;

public interface IProjectRepository
{
    Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> NameExistsAsync(Guid ownerId, string name, CancellationToken cancellationToken = default);
    Task AddAsync(Project project, CancellationToken cancellationToken = default);
    Task<PagedResult<Project>> GetVisibleProjectsAsync(Guid userId, int page, int pageSize, string? search, CancellationToken cancellationToken = default);
}
