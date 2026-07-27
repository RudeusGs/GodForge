using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;

namespace GodForge.Infrastructure.Persistence.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly GodForgeDbContext _context;

    public UserRepository(GodForgeDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = email.ToUpperInvariant();
        return await _context.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalized, cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _context.Users.AddAsync(user, cancellationToken);
    }

    public async Task<bool> AnyAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Users.AnyAsync(cancellationToken);
    }
}
