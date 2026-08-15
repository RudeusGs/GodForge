using GodForge.Application.Common.Models;
using GodForge.Application.Features.Projects.DTOs;

namespace GodForge.Application.Features.Projects;

public interface IProjectLifecycleService
{
    Task<Result<PagedResult<ProjectDto>>> ListAsync(Guid actorId, int page, int pageSize, Guid? organizationId, string? status, string? search, CancellationToken cancellationToken);
    Task<Result<PagedResult<ProjectAdministrationDto>>> ListForOrganizationAsync(Guid actorId, Guid organizationId, int page, int pageSize, CancellationToken cancellationToken);
    Task<Result<ProjectDto>> CreateAsync(Guid actorId, Guid organizationId, string name, string slug, string? description, string visibility, string? idempotencyKey, CancellationToken cancellationToken);
    Task<Result<ProjectDto>> GetAsync(Guid actorId, Guid projectId, CancellationToken cancellationToken);
    Task<Result<ProjectDto>> UpdateAsync(Guid actorId, Guid projectId, string name, string slug, string? description, string visibility, long version, CancellationToken cancellationToken);
    Task<Result<ProjectDeletionAcceptedDto>> RequestDeletionAsync(Guid actorId, Guid projectId, long version, string confirmationSlug, CancellationToken cancellationToken);
    Task<Result<ProjectDto>> RestoreAsync(Guid actorId, Guid projectId, long version, CancellationToken cancellationToken);
}
