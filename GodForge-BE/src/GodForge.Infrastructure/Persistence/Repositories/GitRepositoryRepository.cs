using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Domain.Entities;
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

    public async Task<GitRepository?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.GitRepositories.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }
    
    public async Task AddAsync(GitRepository repository, CancellationToken cancellationToken = default)
    {
        await _context.GitRepositories.AddAsync(repository, cancellationToken);
    }
}
