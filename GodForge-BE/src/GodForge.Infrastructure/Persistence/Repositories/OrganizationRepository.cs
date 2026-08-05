using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models;
using GodForge.Domain.Entities.Core;
using GodForge.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GodForge.Infrastructure.Persistence.Repositories;

public sealed class OrganizationRepository : IOrganizationRepository
{
    private readonly GodForgeDbContext _context;
    public OrganizationRepository(GodForgeDbContext context) => _context = context;

    public Task<Organization?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.Organizations.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<bool> SlugExistsAsync(string slug, Guid? exceptId = null, CancellationToken cancellationToken = default)
        => _context.Organizations.AnyAsync(x => x.Slug == slug && (!exceptId.HasValue || x.Id != exceptId.Value), cancellationToken);

    public async Task<PagedResult<Organization>> GetForMemberAsync(Guid userId, int page, int pageSize, OrganizationStatus? status, CancellationToken cancellationToken = default)
    {
        var query = _context.Organizations.AsNoTracking()
            .Where(x => x.Status != OrganizationStatus.Deleted)
            .Where(x => _context.OrganizationMembers.Any(m => m.OrganizationId == x.Id && m.UserId == userId && m.Status == MembershipStatus.Active));
        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(x => x.Name).ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new PagedResult<Organization>(items, page, pageSize, total);
    }

    public Task AddAsync(Organization organization, CancellationToken cancellationToken = default)
        => _context.Organizations.AddAsync(organization, cancellationToken).AsTask();

    public Task<int> CountCreatedByAsync(Guid userId, CancellationToken cancellationToken = default)
        => _context.Organizations.CountAsync(x => x.CreatedByUserId == userId && x.Status != OrganizationStatus.Deleted, cancellationToken);
}
