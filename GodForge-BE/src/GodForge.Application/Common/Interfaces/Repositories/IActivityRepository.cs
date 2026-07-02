using GodForge.Domain.Entities;
using GodForge.Domain.Entities.Collab;
using GodForge.Application.Common.Models;

namespace GodForge.Application.Common.Interfaces.Repositories;

public interface IActivityRepository
{
    Task AddAsync(Activity activity, CancellationToken cancellationToken = default);
    Task<PagedResult<Activity>> GetProjectActivitiesAsync(Guid projectId, int page, int pageSize, CancellationToken cancellationToken = default);
}
