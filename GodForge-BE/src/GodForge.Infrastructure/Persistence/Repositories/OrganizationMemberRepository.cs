using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models;
using GodForge.Application.Common.Text;
using GodForge.Domain.Entities.Core;
using GodForge.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GodForge.Infrastructure.Persistence.Repositories;

public sealed class OrganizationMemberRepository : IOrganizationMemberRepository
{
    private readonly GodForgeDbContext _context;
    public OrganizationMemberRepository(GodForgeDbContext context) => _context = context;

    public Task<OrganizationMember?> GetAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken = default)
        => _context.OrganizationMembers.FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.UserId == userId, cancellationToken);

    public async Task<IReadOnlyList<OrganizationMember>> GetForOrganizationsAsync(
        IReadOnlyCollection<Guid> organizationIds,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (organizationIds.Count == 0)
            return Array.Empty<OrganizationMember>();

        return await _context.OrganizationMembers
            .AsNoTracking()
            .Where(member => organizationIds.Contains(member.OrganizationId) &&
                             member.UserId == userId &&
                             member.Status == MembershipStatus.Active)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> IsActiveAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken = default)
        => _context.OrganizationMembers.AnyAsync(x => x.OrganizationId == organizationId && x.UserId == userId && x.Status == MembershipStatus.Active, cancellationToken);

    public Task<int> GetActiveOwnerCountAsync(Guid organizationId, CancellationToken cancellationToken = default)
        => _context.OrganizationMembers.CountAsync(x => x.OrganizationId == organizationId && x.Role == OrganizationRole.OrganizationOwner && x.Status == MembershipStatus.Active, cancellationToken);

    public async Task<PagedResult<OrganizationMember>> GetForOrganizationAsync(Guid organizationId, int page, int pageSize, string? role, string? status, string? search, CancellationToken cancellationToken = default)
    {
        var query = _context.OrganizationMembers.AsNoTracking().Where(x => x.OrganizationId == organizationId);
        query = query.Where(x => _context.Users.Any(user => user.Id == x.UserId));
        if (EnumText.TryParseDefined<OrganizationRole>(role, out var parsedRole))
            query = query.Where(x => x.Role == parsedRole);
        if (EnumText.TryParseDefined<MembershipStatus>(status, out var parsedStatus))
            query = query.Where(x => x.Status == parsedStatus);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedEmailPattern = PostgresSearch.ContainsPattern(search.ToUpperInvariant());
            var displayNamePattern = PostgresSearch.ContainsPattern(search);
            query = query.Where(x => _context.Users.Any(u => u.Id == x.UserId &&
                (EF.Functions.Like(u.NormalizedEmail, normalizedEmailPattern, PostgresSearch.LikeEscapeCharacter) ||
                 EF.Functions.ILike(u.DisplayName, displayNamePattern, PostgresSearch.LikeEscapeCharacter))));
        }
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(x => x.Role).ThenBy(x => x.JoinedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new PagedResult<OrganizationMember>(items, page, pageSize, total);
    }

    public Task AddAsync(OrganizationMember membership, CancellationToken cancellationToken = default)
        => _context.OrganizationMembers.AddAsync(membership, cancellationToken).AsTask();
}
