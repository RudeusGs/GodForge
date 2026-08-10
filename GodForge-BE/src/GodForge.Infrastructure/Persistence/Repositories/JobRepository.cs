using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models;
using GodForge.Domain.Entities.Ops;
using GodForge.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GodForge.Infrastructure.Persistence.Repositories;

public sealed class JobRepository : IJobRepository
{
    private readonly GodForgeDbContext _context;

    public JobRepository(GodForgeDbContext context)
    {
        _context = context;
    }

    public async Task<Job?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Jobs.FirstOrDefaultAsync(j => j.Id == id, cancellationToken);
    }


    public async Task<Job?> TryClaimAsync(
        Guid id,
        DateTimeOffset now,
        TimeSpan staleAfter,
        CancellationToken cancellationToken = default)
    {
        if (staleAfter <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(staleAfter));

        var staleBefore = now.Subtract(staleAfter);
        var claimToken = Guid.NewGuid();
        var affected = await _context.Jobs
            .Where(job => job.Id == id &&
                (((job.Status == JobStatus.Queued || job.Status == JobStatus.Retrying) && job.AvailableAt <= now) ||
                 (job.Status == JobStatus.Running &&
                  (job.LastHeartbeatAt == null || job.LastHeartbeatAt <= staleBefore))))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(job => job.Status, JobStatus.Running)
                .SetProperty(job => job.StartedAt, job => job.StartedAt ?? (DateTimeOffset?)now)
                .SetProperty(job => job.LastHeartbeatAt, now)
                .SetProperty(job => job.ClaimToken, claimToken)
                .SetProperty(job => job.AttemptCount, job => job.AttemptCount + 1)
                .SetProperty(job => job.ErrorCode, (string?)null)
                .SetProperty(job => job.ErrorMessage, (string?)null)
                .SetProperty(job => job.UpdatedAt, now),
                cancellationToken);

        if (affected == 0)
            return null;

        // Once the atomic UPDATE succeeds, always materialize the claimed row so the
        // caller can release the lease even if host shutdown was requested concurrently.
        return await _context.Jobs.FirstAsync(job => job.Id == id, CancellationToken.None);
    }

    public async Task<bool> TryHeartbeatAsync(
        Guid id,
        Guid claimToken,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        if (claimToken == Guid.Empty)
            throw new ArgumentException("A non-empty job claim token is required.", nameof(claimToken));

        var affected = await _context.Jobs
            .Where(job =>
                job.Id == id &&
                job.Status == JobStatus.Running &&
                job.ClaimToken == claimToken)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(job => job.LastHeartbeatAt, now)
                    .SetProperty(job => job.UpdatedAt, now),
                cancellationToken);

        return affected == 1;
    }

    public Task<bool> IsCancellationRequestedAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.Jobs
            .AsNoTracking()
            .Where(job => job.Id == id)
            .Select(job => job.CancellationRequestedAt != null)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task AddAsync(Job job, CancellationToken cancellationToken = default)
    {
        await _context.Jobs.AddAsync(job, cancellationToken);
    }

    public Task<Job?> GetByIdempotencyKeyAsync(Guid projectId, GodForge.Domain.Enums.JobType type, string idempotencyKey, CancellationToken cancellationToken = default)
    {
        return _context.Jobs.FirstOrDefaultAsync(
            job => job.ProjectId == projectId && job.Type == type && job.IdempotencyKey == idempotencyKey,
            cancellationToken);
    }

    public async Task<PagedResult<Job>> GetProjectJobsAsync(Guid projectId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.Jobs.Where(j => j.ProjectId == projectId);

        var totalItems = await query.CountAsync(cancellationToken);

        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        if (page > totalPages)
        {
            page = totalPages > 0 ? totalPages : 1;
        }

        var items = await query
            .AsNoTracking()
            .OrderByDescending(j => j.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Job>(items, page, pageSize, totalItems);
    }
}
