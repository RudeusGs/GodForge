using GodForge.Application.Common.Interfaces;

namespace GodForge.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly GodForgeDbContext _context;

    public UnitOfWork(GodForgeDbContext context)
    {
        _context = context;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);

    public void ClearTrackedChanges()
        => _context.ChangeTracker.Clear();
}
