using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models;
using GodForge.Domain.Entities;
using GodForge.Domain.Entities.Core;
using GodForge.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GodForge.Infrastructure.Persistence.Repositories;

public sealed class ProjectRepository : IProjectRepository
{
    private readonly GodForgeDbContext _context;

    public ProjectRepository(GodForgeDbContext context)
    {
        _context = context;
    }

    public async Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Projects.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<bool> NameExistsAsync(Guid ownerId, string name, CancellationToken cancellationToken = default)
    {
        var slug = name.ToLowerInvariant().Replace(" ", "-");
        return await _context.Projects.AnyAsync(p => p.CreatedBy == ownerId && p.Slug == slug, cancellationToken);
    }

    public async Task AddAsync(Project project, CancellationToken cancellationToken = default)
    {
        await _context.Projects.AddAsync(project, cancellationToken);
    }

    public async Task<PagedResult<Project>> GetVisibleProjectsAsync(Guid userId, int page, int pageSize, string? search, CancellationToken cancellationToken = default)
    {
        // Active projects where user is a member OR project is public/internal
        var query = _context.Projects
            .Where(p => p.Status != ProjectStatus.Deleted)
            .Where(p => 
                p.Visibility == ProjectVisibility.Public || 
                p.Visibility == ProjectVisibility.Internal || 
                _context.ProjectMembers.Any(m => m.ProjectId == p.Id && m.UserId == userId));

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p => p.Name.Contains(search) || (p.Description != null && p.Description.Contains(search)));
        }

        var totalItems = await query.CountAsync(cancellationToken);
        
        var items = await query
            .AsNoTracking()
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Project>(items, page, pageSize, totalItems);
    }
}
