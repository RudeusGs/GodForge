using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Domain.Entities;
using GodForge.Domain.Entities.Repo;
using Microsoft.EntityFrameworkCore;

namespace GodForge.Infrastructure.Persistence.Repositories;

public sealed class RepositoryRepository : IRepositoryRepository
{
    private readonly GodForgeDbContext _context;

    public RepositoryRepository(GodForgeDbContext context)
    {
        _context = context;
    }

    public async Task<Repository?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Repositories.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }
    
    public async Task AddAsync(Repository repository, CancellationToken cancellationToken = default)
    {
        await _context.Repositories.AddAsync(repository, cancellationToken);
    }
}
