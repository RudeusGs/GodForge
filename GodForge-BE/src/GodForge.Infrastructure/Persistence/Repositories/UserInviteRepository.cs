using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models;
using GodForge.Domain.Entities.Identity;
using GodForge.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GodForge.Infrastructure.Persistence.Repositories;

public sealed class UserInviteRepository : IUserInviteRepository
{
    private readonly GodForgeDbContext _context;

    public UserInviteRepository(GodForgeDbContext context) => _context = context;

    public Task<UserInvite?> GetByIdAsync(Guid organizationId, Guid invitationId, CancellationToken cancellationToken = default)
        => _context.UserInvites.FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == invitationId, cancellationToken);

    public Task<UserInvite?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
        => _context.UserInvites.FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

    public Task<UserInvite?> GetPendingAsync(Guid organizationId, string normalizedEmail, CancellationToken cancellationToken = default)
        => _context.UserInvites.FirstOrDefaultAsync(x => x.OrganizationId == organizationId && x.NormalizedEmail == normalizedEmail && x.Status == InviteStatus.Pending, cancellationToken);

    public async Task<PagedResult<UserInvite>> GetForOrganizationAsync(Guid organizationId, int page, int pageSize, string? status, string? email, CancellationToken cancellationToken = default)
    {
        var query = _context.UserInvites.AsNoTracking().Where(x => x.OrganizationId == organizationId);
        if (Enum.TryParse<InviteStatus>(status, true, out var parsedStatus))
            query = query.Where(x => x.Status == parsedStatus);
        if (!string.IsNullOrWhiteSpace(email))
        {
            var normalized = User.NormalizeEmail(email);
            query = query.Where(x => x.NormalizedEmail.Contains(normalized));
        }
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new PagedResult<UserInvite>(items, page, pageSize, total);
    }

    public Task AddAsync(UserInvite invitation, CancellationToken cancellationToken = default)
        => _context.UserInvites.AddAsync(invitation, cancellationToken).AsTask();

    public Task<int> CountPendingAsync(Guid organizationId, CancellationToken cancellationToken = default)
        => _context.UserInvites.CountAsync(x => x.OrganizationId == organizationId && x.Status == InviteStatus.Pending, cancellationToken);
}
