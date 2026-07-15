using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Domain.Entities.Repo;
using Microsoft.EntityFrameworkCore;

namespace GodForge.Infrastructure.Persistence.Repositories;

public sealed class RepositorySnapshotRepository : IRepositorySnapshotRepository
{
    private readonly GodForgeDbContext _context;

    public RepositorySnapshotRepository(GodForgeDbContext context)
    {
        _context = context;
    }

    public Task<RepositorySnapshot?> GetByCommitAsync(
        Guid repositoryId,
        string commitSha,
        CancellationToken cancellationToken = default)
        => _context.RepositorySnapshots.FirstOrDefaultAsync(
            snapshot => snapshot.RepositoryId == repositoryId && snapshot.CommitHash == commitSha,
            cancellationToken);

    public async Task AddAsync(RepositorySnapshot snapshot, CancellationToken cancellationToken = default)
        => await _context.RepositorySnapshots.AddAsync(snapshot, cancellationToken);
}
