using GodForge.Application.Common.Models;
using GodForge.Domain.Entities.Ops;
using GodForge.Domain.Enums;

namespace GodForge.Application.Common.Interfaces.Repositories;

public interface IJobRepository
{
    Task<Job?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Job?> TryClaimAsync(Guid id, DateTimeOffset now, TimeSpan staleAfter, CancellationToken cancellationToken = default);
    Task<bool> TryHeartbeatAsync(Guid id, Guid claimToken, DateTimeOffset now, CancellationToken cancellationToken = default);
    Task<bool> IsCancellationRequestedAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Job job, CancellationToken cancellationToken = default);
    Task<Job?> GetByIdempotencyKeyAsync(Guid projectId, JobType type, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<PagedResult<Job>> GetProjectJobsAsync(Guid projectId, int page, int pageSize, CancellationToken cancellationToken = default);
}
