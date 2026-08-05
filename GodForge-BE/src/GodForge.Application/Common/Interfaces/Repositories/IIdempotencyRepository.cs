using GodForge.Domain.Entities.Ops;

namespace GodForge.Application.Common.Interfaces.Repositories;

public interface IIdempotencyRepository
{
    Task<IdempotencyRecord?> GetAsync(Guid actorUserId, string operation, string key, CancellationToken cancellationToken = default);
    Task AddAsync(IdempotencyRecord record, CancellationToken cancellationToken = default);
}
