using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Domain.Entities.Core;
using GodForge.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GodForge.Infrastructure.Persistence.Repositories;

public sealed class ProjectMemberRepository : IProjectMemberRepository
{
    private readonly GodForgeDbContext _context;

    public ProjectMemberRepository(GodForgeDbContext context)
    {
        _context = context;
    }

    public async Task<ProjectMember?> GetMembershipAsync(Guid projectId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.ProjectMembers
            .FirstOrDefaultAsync(m => m.ProjectId == projectId && m.UserId == userId && m.RemovedAt == null, cancellationToken);
    }

    public async Task AddAsync(ProjectMember member, CancellationToken cancellationToken = default)
    {
        await _context.ProjectMembers.AddAsync(member, cancellationToken);
    }

    public async Task<int> GetOwnerCountAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await _context.ProjectMembers
            .CountAsync(m => m.ProjectId == projectId && m.Role == ProjectRole.ProjectOwner && m.RemovedAt == null, cancellationToken);
    }
}
