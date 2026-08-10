using GodForge.Application.Common.Models;
using GodForge.Domain.Entities.Core;

namespace GodForge.Application.Common.Interfaces.Repositories;

public interface IProjectRepository
{
    Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> NameExistsAsync(Guid organizationId, string name, Guid? exceptProjectId = null, CancellationToken cancellationToken = default);
    Task<bool> SlugExistsAsync(Guid organizationId, string slug, Guid? exceptProjectId = null, CancellationToken cancellationToken = default);
    Task AddAsync(Project project, CancellationToken cancellationToken = default);
    Task AddSettingsAsync(ProjectSetting settings, CancellationToken cancellationToken = default);
    Task<ProjectSetting?> GetSettingsAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<PagedResult<Project>> GetVisibleProjectsAsync(Guid userId, int page, int pageSize, string? search, Guid? organizationId = null, string? status = null, CancellationToken cancellationToken = default);
    Task<PagedResult<Project>> GetForOrganizationAsync(Guid organizationId, Guid userId, bool includeAll, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> CountForOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default);
}
