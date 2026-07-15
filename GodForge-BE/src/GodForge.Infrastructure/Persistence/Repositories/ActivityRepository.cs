using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models;
using GodForge.Domain.Entities.Collab;
using Microsoft.EntityFrameworkCore;

namespace GodForge.Infrastructure.Persistence.Repositories;

public sealed class ActivityRepository : IActivityRepository
{
    private readonly GodForgeDbContext _context;

    public ActivityRepository(GodForgeDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Activity activity, CancellationToken cancellationToken = default)
    {
        await _context.Activities.AddAsync(activity, cancellationToken);
    }

    public async Task<PagedResult<Activity>> GetProjectActivitiesAsync(Guid projectId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.Activities.Where(a => a.ProjectId == projectId);

        var totalItems = await query.CountAsync(cancellationToken);

        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        if (page > totalPages)
        {
            page = totalPages > 0 ? totalPages : 1;
        }

        var items = await query
            .AsNoTracking()
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Activity>(items, page, pageSize, totalItems);
    }
}
