using GodForge.Application.Common.Interfaces;
using GodForge.Infrastructure.Persistence;

namespace GodForge.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly GodForgeDbContext _context;

    public UnitOfWork(GodForgeDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
