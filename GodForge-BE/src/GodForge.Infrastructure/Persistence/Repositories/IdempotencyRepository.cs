using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Domain.Entities.Ops;
using Microsoft.EntityFrameworkCore;

namespace GodForge.Infrastructure.Persistence.Repositories;

public sealed class IdempotencyRepository : IIdempotencyRepository
{
    private readonly GodForgeDbContext _context;

    public IdempotencyRepository(GodForgeDbContext context) => _context = context;

    public Task<IdempotencyRecord?> GetAsync(Guid actorUserId, string operation, string key, CancellationToken cancellationToken = default)
        => _context.IdempotencyRecords.AsNoTracking().FirstOrDefaultAsync(
            x => x.ActorUserId == actorUserId && x.Operation == operation && x.Key == key,
            cancellationToken);

    public Task AddAsync(IdempotencyRecord record, CancellationToken cancellationToken = default)
        => _context.IdempotencyRecords.AddAsync(record, cancellationToken).AsTask();
}
