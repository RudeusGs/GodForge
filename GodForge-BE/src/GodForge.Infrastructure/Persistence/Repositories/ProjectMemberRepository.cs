using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models;
using GodForge.Application.Common.Text;
using GodForge.Domain.Entities.Core;
using GodForge.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GodForge.Infrastructure.Persistence.Repositories;

public sealed class ProjectMemberRepository : IProjectMemberRepository
{
    private readonly GodForgeDbContext _context;
    public ProjectMemberRepository(GodForgeDbContext context) => _context = context;

    public Task<ProjectMember?> GetMembershipAsync(Guid projectId, Guid userId, CancellationToken cancellationToken = default)
        => _context.ProjectMembers.FirstOrDefaultAsync(m => m.ProjectId == projectId && m.UserId == userId && m.Status == MembershipStatus.Active, cancellationToken);

    public Task<ProjectMember?> GetAnyMembershipAsync(Guid projectId, Guid userId, CancellationToken cancellationToken = default)
        => _context.ProjectMembers.FirstOrDefaultAsync(m => m.ProjectId == projectId && m.UserId == userId, cancellationToken);

    public async Task<IReadOnlyList<ProjectMember>> GetMembershipsAsync(
        IReadOnlyCollection<Guid> projectIds,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (projectIds.Count == 0)
            return Array.Empty<ProjectMember>();

        return await _context.ProjectMembers
            .AsNoTracking()
            .Where(member => projectIds.Contains(member.ProjectId) &&
                             member.UserId == userId &&
                             member.Status == MembershipStatus.Active)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProjectMemberStatistics>> GetStatisticsAsync(
        IReadOnlyCollection<Guid> projectIds,
        CancellationToken cancellationToken = default)
    {
        if (projectIds.Count == 0)
            return Array.Empty<ProjectMemberStatistics>();

        return await _context.ProjectMembers
            .AsNoTracking()
            .Where(member => projectIds.Contains(member.ProjectId) && member.Status == MembershipStatus.Active)
            .GroupBy(member => member.ProjectId)
            .Select(group => new ProjectMemberStatistics(
                group.Key,
                group.Count(member => member.Role == ProjectRole.ProjectOwner),
                group.Count()))
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(ProjectMember member, CancellationToken cancellationToken = default)
        => _context.ProjectMembers.AddAsync(member, cancellationToken).AsTask();

    public Task<int> GetOwnerCountAsync(Guid projectId, CancellationToken cancellationToken = default)
        => _context.ProjectMembers.CountAsync(m => m.ProjectId == projectId && m.Role == ProjectRole.ProjectOwner && m.Status == MembershipStatus.Active, cancellationToken);

    public Task<int> GetActiveCountAsync(Guid projectId, CancellationToken cancellationToken = default)
        => _context.ProjectMembers.CountAsync(m => m.ProjectId == projectId && m.Status == MembershipStatus.Active, cancellationToken);

    public async Task<PagedResult<ProjectMember>> GetForProjectAsync(Guid projectId, int page, int pageSize, string? role, string? status, string? search, CancellationToken cancellationToken = default)
    {
        var query = _context.ProjectMembers.AsNoTracking().Where(x => x.ProjectId == projectId);
        query = query.Where(x => _context.Users.Any(user => user.Id == x.UserId));
        if (EnumText.TryParseDefined<ProjectRole>(role, out var parsedRole))
            query = query.Where(x => x.Role == parsedRole);
        if (EnumText.TryParseDefined<MembershipStatus>(status, out var parsedStatus))
            query = query.Where(x => x.Status == parsedStatus);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalized = search.Trim().ToUpperInvariant();
            query = query.Where(x => _context.Users.Any(u => u.Id == x.UserId &&
                (u.NormalizedEmail.Contains(normalized) || EF.Functions.ILike(u.DisplayName, $"%{search.Trim()}%"))));
        }
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(x => x.Role).ThenBy(x => x.JoinedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new PagedResult<ProjectMember>(items, page, pageSize, total);
    }

    public async Task<IReadOnlyList<ProjectMember>> GetActiveByOrganizationUserAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken = default)
        => await _context.ProjectMembers
            .AsNoTracking()
            .Where(m => m.OrganizationId == organizationId && m.UserId == userId && m.Status == MembershipStatus.Active)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Guid>> GetSoleOwnerProjectIdsAsync(
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken = default)
        => await _context.ProjectMembers
            .AsNoTracking()
            .Where(member => member.OrganizationId == organizationId &&
                             member.UserId == userId &&
                             member.Role == ProjectRole.ProjectOwner &&
                             member.Status == MembershipStatus.Active &&
                             !_context.ProjectMembers.Any(other =>
                                 other.ProjectId == member.ProjectId &&
                                 other.UserId != userId &&
                                 other.Role == ProjectRole.ProjectOwner &&
                                 other.Status == MembershipStatus.Active))
            .Select(member => member.ProjectId)
            .Distinct()
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Guid>> SuspendAllForOrganizationUserAsync(Guid organizationId, Guid userId, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var query = _context.ProjectMembers
            .Where(m => m.OrganizationId == organizationId && m.UserId == userId && m.Status == MembershipStatus.Active);
        var projectIds = await query.Select(m => m.ProjectId).Distinct().ToListAsync(cancellationToken);
        await query.ExecuteUpdateAsync(setters => setters
            .SetProperty(m => m.Status, MembershipStatus.Suspended)
            .SetProperty(m => m.SuspendedAt, now)
            .SetProperty(m => m.RemovedAt, (DateTimeOffset?)null)
            .SetProperty(m => m.Version, m => m.Version + 1)
            .SetProperty(m => m.UpdatedAt, now), cancellationToken);
        return projectIds;
    }

    public async Task<IReadOnlyList<Guid>> RemoveAllForOrganizationUserAsync(Guid organizationId, Guid userId, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var query = _context.ProjectMembers
            .Where(m => m.OrganizationId == organizationId && m.UserId == userId && m.Status != MembershipStatus.Removed);
        var projectIds = await query.Select(m => m.ProjectId).Distinct().ToListAsync(cancellationToken);
        await query.ExecuteUpdateAsync(setters => setters
            .SetProperty(m => m.Status, MembershipStatus.Removed)
            .SetProperty(m => m.RemovedAt, now)
            .SetProperty(m => m.SuspendedAt, (DateTimeOffset?)null)
            .SetProperty(m => m.Version, m => m.Version + 1)
            .SetProperty(m => m.UpdatedAt, now), cancellationToken);
        return projectIds;
    }
}
