using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models;
using GodForge.Domain.Entities.Core;
using GodForge.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GodForge.Infrastructure.Persistence.Repositories;

public sealed class ProjectRepository : IProjectRepository
{
    private readonly GodForgeDbContext _context;
    public ProjectRepository(GodForgeDbContext context) => _context = context;

    public Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.Projects.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<bool> NameExistsAsync(Guid organizationId, string name, CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim().ToUpperInvariant();
        return _context.Projects.AnyAsync(project => project.OrganizationId == organizationId && project.Name.ToUpper() == normalizedName, cancellationToken);
    }

    public Task<bool> SlugExistsAsync(Guid organizationId, string slug, Guid? exceptProjectId = null, CancellationToken cancellationToken = default)
        => _context.Projects.AnyAsync(project => project.OrganizationId == organizationId && project.Slug == slug && (!exceptProjectId.HasValue || project.Id != exceptProjectId.Value), cancellationToken);

    public Task AddAsync(Project project, CancellationToken cancellationToken = default)
        => _context.Projects.AddAsync(project, cancellationToken).AsTask();

    public Task AddSettingsAsync(ProjectSetting settings, CancellationToken cancellationToken = default)
        => _context.ProjectSettings.AddAsync(settings, cancellationToken).AsTask();

    public Task<ProjectSetting?> GetSettingsAsync(Guid projectId, CancellationToken cancellationToken = default)
        => _context.ProjectSettings.FirstOrDefaultAsync(x => x.ProjectId == projectId, cancellationToken);

    public async Task<PagedResult<Project>> GetVisibleProjectsAsync(Guid userId, int page, int pageSize, string? search, Guid? organizationId = null, string? status = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Projects
            .Where(p => p.Status != ProjectStatus.Deleted)
            .Where(p => _context.ProjectMembers.Any(m => m.ProjectId == p.Id && m.OrganizationId == p.OrganizationId && m.UserId == userId && m.Status == MembershipStatus.Active) &&
                        _context.OrganizationMembers.Any(m => m.OrganizationId == p.OrganizationId && m.UserId == userId && m.Status == MembershipStatus.Active));
        if (organizationId.HasValue)
            query = query.Where(x => x.OrganizationId == organizationId.Value);
        if (Enum.TryParse<ProjectStatus>(status, true, out var parsedStatus))
            query = query.Where(x => x.Status == parsedStatus);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.Name.Contains(search) || (p.Description != null && p.Description.Contains(search)));
        var total = await query.CountAsync(cancellationToken);
        var items = await query.AsNoTracking().OrderByDescending(p => p.UpdatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new PagedResult<Project>(items, page, pageSize, total);
    }

    public async Task<PagedResult<Project>> GetForOrganizationAsync(Guid organizationId, Guid userId, bool includeAll, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.Projects.AsNoTracking().Where(x => x.OrganizationId == organizationId && x.Status != ProjectStatus.Deleted);
        if (!includeAll)
            query = query.Where(x => _context.ProjectMembers.Any(m => m.ProjectId == x.Id && m.UserId == userId && m.Status == MembershipStatus.Active));
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(x => x.Name).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new PagedResult<Project>(items, page, pageSize, total);
    }

    public Task<int> CountForOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default)
        => _context.Projects.CountAsync(x => x.OrganizationId == organizationId && x.Status != ProjectStatus.Deleted, cancellationToken);
}
