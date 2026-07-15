using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Domain.Entities.Repo;
using Microsoft.EntityFrameworkCore;

namespace GodForge.Infrastructure.Persistence.Repositories;

public sealed class GitRepositoryRepository : IGitRepositoryRepository
{
    private readonly GodForgeDbContext _context;

    public GitRepositoryRepository(GodForgeDbContext context)
    {
        _context = context;
    }

    public Task<GitRepository?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.GitRepositories.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public Task<GitRepository?> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
        => _context.GitRepositories.FirstOrDefaultAsync(r => r.ProjectId == projectId, cancellationToken);

    public async Task AddAsync(GitRepository repository, CancellationToken cancellationToken = default)
        => await _context.GitRepositories.AddAsync(repository, cancellationToken);
}
