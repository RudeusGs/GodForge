using GodForge.Application.Common.Models;
using GodForge.Domain.Entities;
using GodForge.Domain.Entities.Collab;

namespace GodForge.Application.Common.Interfaces.Repositories;

public interface IActivityRepository
{
    Task AddAsync(Activity activity, CancellationToken cancellationToken = default);
    Task<PagedResult<Activity>> GetProjectActivitiesAsync(Guid projectId, int page, int pageSize, CancellationToken cancellationToken = default);
}
