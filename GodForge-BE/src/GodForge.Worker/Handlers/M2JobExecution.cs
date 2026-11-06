using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models;
using GodForge.Application.Common.Models.Messages;
using GodForge.Domain.Entities.Ops;
using GodForge.Domain.Enums;

namespace GodForge.Worker.Handlers;

/// <summary>Short durable claim/finalize boundaries for the existing M2 queues.</summary>
public sealed class M2JobExecution(IJobRepository jobs, IUnitOfWork unitOfWork, IOutboxWriter outbox,
    IRepositoryLockProvider repositoryLocks, IHostedRepositoryAccessStore access, IServiceScopeFactory scopes, IClock clock, ILogger<M2JobExecution> logger)
{
    public static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(3);
    public async Task<JobExecutionResult> RunAsync<TInput, TOutput>(WorkerMessage message, JobType type, string queue,
        Func<Job, CancellationToken, Task<TInput>> prepare,
        Func<TInput, CancellationToken, Task<TOutput>> execute,
        Func<Job, TInput, TOutput, CancellationToken, Task> finalize,
        Func<string, CancellationToken, Task> recordFailure,
        CancellationToken cancellationToken, bool serializeRepository = true) where TInput : class where TOutput : class
    {
        if (message.SchemaVersion != "1.0" || message.JobId == Guid.Empty || message.RepositoryId is null ||
            message.ProjectId == Guid.Empty || message.OrganizationId == Guid.Empty)
            return JobExecutionResult.DeadLetter();

        // This session lock serializes repository operations, not membership writes.
        // No transaction is open while waiting for it or during provider networking.
        await using var repositoryLock = serializeRepository
            ? await repositoryLocks.AcquireOperationAsync(message.RepositoryId.Value, TimeSpan.FromSeconds(15), cancellationToken) : null;
        Job? job;
        TInput input;
        await BeginAsync(message, cancellationToken);
        try
        {
            job = await jobs.GetByIdAsync(message.JobId, cancellationToken);
            if (job is null || job.Type != type || job.QueueName != queue || job.ProjectId != message.ProjectId || job.RepositoryId != message.RepositoryId ||
                job.IdempotencyKey is not null && job.IdempotencyKey != message.InputHash)
                return JobExecutionResult.DeadLetter();
            if (Terminal(job.Status)) return JobExecutionResult.Completed();
            if (job.CancellationRequestedAt is not null) { job.Cancel(clock.UtcNow); await CommitAsync(cancellationToken); return JobExecutionResult.Completed(); }
            if (job.AttemptCount >= job.MaxAttempts &&
                (job.Status != JobStatus.Running || job.LastHeartbeatAt is null || job.LastHeartbeatAt.Value.Add(LeaseDuration) <= clock.UtcNow))
            {
                job.MarkDeadLettered("JOB_RETRY_EXHAUSTED", "M2 retry budget exhausted.", clock.UtcNow);
                await CommitAsync(cancellationToken);
                return JobExecutionResult.DeadLetter();
            }
            unitOfWork.ClearTrackedChanges();
            job = await jobs.TryClaimAsync(message.JobId, clock.UtcNow, LeaseDuration, cancellationToken);
            if (job is null)
            {
                await ScheduleAsync(queue, message, clock.UtcNow.Add(LeaseDuration), cancellationToken);
                await CommitAsync(cancellationToken);
                return JobExecutionResult.Completed();
            }
            try { input = await prepare(job, cancellationToken); }
            catch (HostedGitException e)
            {
                job.MarkDeadLettered(e.Code, "M2 operation is not eligible.", clock.UtcNow);
                await recordFailure(e.Code, cancellationToken);
                await CommitAsync(cancellationToken);
                return JobExecutionResult.DeadLetter();
            }
            // Recovery survives process death even if the original delivery is lost/repeated.
            await ScheduleAsync(queue, message, clock.UtcNow.Add(LeaseDuration), cancellationToken);
            await CommitAsync(cancellationToken);
        }
        finally { await unitOfWork.RollbackTransactionAsync(CancellationToken.None); }

        var claim = job.ClaimToken!.Value;
        TOutput? output = null;
        HostedGitException? failure = null;
        using var execution = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        execution.CancelAfter(TimeSpan.FromSeconds(120));
        var heartbeat = HeartbeatAsync(job.Id, claim, execution);
        try { output = await execute(input, execution.Token); }
        catch (HostedGitException e) { failure = e; }
        catch (OperationCanceledException) { failure = new("M2_OPERATION_INTERRUPTED", true); }
        catch (Exception e) when (e is KeyNotFoundException or InvalidOperationException or FormatException)
        { failure = new("FORGEJO_RESPONSE_INVALID"); }
        catch (TimeoutException) { failure = new("FORGEJO_UNAVAILABLE", true); }
        finally { await execution.CancelAsync(); await heartbeat; }

        // Shutdown still gets a bounded opportunity to persist retry intent.
        using var completion = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        var finalToken = completion.Token;
        await BeginAsync(message, finalToken);
        try
        {
            var current = await jobs.GetByIdAsync(message.JobId, finalToken);
            if (current is null || current.Status != JobStatus.Running || current.ClaimToken != claim ||
                current.LastHeartbeatAt is null || current.LastHeartbeatAt.Value.Add(LeaseDuration) <= clock.UtcNow)
            {
                logger.LogWarning("M2 stale claim discarded for job {JobId}", message.JobId);
                // A timed-out holder may have changed the provider after a newer
                // holder's verification. Fence business output and request repair.
                if (type is JobType.ProvisionHostedRepository or JobType.ReconcileHostedRepository)
                {
                    var scope = await access.ReadAsync(message.RepositoryId.Value, finalToken);
                    if (scope?.Repository.ProjectId == message.ProjectId && scope.OrganizationId == message.OrganizationId)
                    {
                        scope.Repository.RequestAccessReconciliation(clock.UtcNow);
                        await QueueReconciliationAsync(scope.Repository.Id, message.ProjectId, message.OrganizationId, message.CorrelationId, finalToken);
                        await CommitAsync(finalToken);
                    }
                }
                return JobExecutionResult.Completed();
            }
            if (failure is null)
            {
                try { await finalize(current, input, output!, finalToken); current.MarkCompleted(null, clock.UtcNow); }
                catch (HostedGitException e) { failure = e; }
            }
            if (failure is not null)
            {
                await recordFailure(failure.Code, finalToken);
                if (failure.Retryable && current.AttemptCount < current.MaxAttempts)
                {
                    var next = clock.UtcNow.AddSeconds(Math.Min(300, 5 * Math.Pow(2, current.AttemptCount)) + Random.Shared.Next(0, 5));
                    current.MarkRetrying(failure.Code, "M2 provider operation pending retry.", next, clock.UtcNow);
                    await ScheduleAsync(queue, message, next, finalToken);
                }
                else current.MarkDeadLettered(failure.Code, "M2 operation requires attention.", clock.UtcNow);
                logger.LogWarning("M2 job {JobId} failed with {ErrorCode}; attempt {Attempt}", current.Id, failure.Code, current.AttemptCount);
            }
            await CommitAsync(finalToken);
            return current.Status == JobStatus.DeadLettered ? JobExecutionResult.DeadLetter() : JobExecutionResult.Completed();
        }
        finally { await unitOfWork.RollbackTransactionAsync(CancellationToken.None); }
    }

    public async Task QueueReconciliationAsync(Guid repositoryId, Guid projectId, Guid organizationId, string correlationId, CancellationToken token)
    {
        var now = clock.UtcNow;
        var job = Job.Create(projectId, repositoryId, JobType.ReconcileHostedRepository, "repository.hosted.reconcile", 0, null, null, 5, null, correlationId, now, now);
        await jobs.AddAsync(job, token);
        await outbox.EnqueueAsync(job.QueueName, new HostedRepositoryReconcileJobMessage
        { JobId = job.Id, RepositoryId = repositoryId, ProjectId = projectId, OrganizationId = organizationId, CorrelationId = correlationId, InputHash = $"access:{repositoryId:N}", CreatedAt = now }, token);
    }

    private async Task BeginAsync(WorkerMessage message, CancellationToken token)
    {
        unitOfWork.ClearTrackedChanges();
        await unitOfWork.BeginTransactionAsync(token);
        try
        {
            await unitOfWork.AcquireResourceLockAsync("organization-membership", message.OrganizationId, token);
            await unitOfWork.AcquireResourceLockAsync("organization-project-catalog", message.OrganizationId, token);
            await unitOfWork.AcquireResourceLockAsync("project-membership", message.ProjectId, token);
            await unitOfWork.AcquireResourceLockAsync("m2-job", message.JobId, token);
        }
        catch { await unitOfWork.RollbackTransactionAsync(CancellationToken.None); throw; }
    }
    private async Task CommitAsync(CancellationToken token) { await unitOfWork.SaveChangesAsync(token); await unitOfWork.CommitTransactionAsync(token); }
    private Task ScheduleAsync(string queue, WorkerMessage message, DateTimeOffset available, CancellationToken token)
        => outbox.EnqueueScheduledAsync(queue, message switch
        {
            HostedRepositoryProvisionJobMessage p => p with { MessageId = Guid.NewGuid() },
            HostedRepositoryReconcileJobMessage r => r with { MessageId = Guid.NewGuid() },
            RepositorySyncJobMessage s => s with { MessageId = Guid.NewGuid() },
            _ => throw new InvalidOperationException("Unsupported M2 queue.")
        }, available, token);
    private async Task HeartbeatAsync(Guid id, Guid claim, CancellationTokenSource execution)
    {
        try
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));
            while (await timer.WaitForNextTickAsync(execution.Token))
            {
                using var scope = scopes.CreateScope();
                if (!await scope.ServiceProvider.GetRequiredService<IJobRepository>().TryHeartbeatAsync(id, claim, clock.UtcNow, execution.Token))
                { await execution.CancelAsync(); return; }
            }
        }
        catch (OperationCanceledException) when (execution.IsCancellationRequested) { }
        catch (Exception) { logger.LogWarning("M2 heartbeat failed for job {JobId}", id); await execution.CancelAsync(); }
    }
    private static bool Terminal(JobStatus status) => status is JobStatus.Completed or JobStatus.Cancelled or JobStatus.Failed or JobStatus.DeadLettered or JobStatus.Timeout;
}
