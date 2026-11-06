using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models.Messages;
using GodForge.Domain.Enums;
using GodForge.Worker.Handlers;

namespace GodForge.Worker.Queues;

public sealed class WorkerDeliveryRecovery(IJobRepository jobs, IUnitOfWork unitOfWork, IOutboxWriter outbox, IClock clock)
{
    public async Task<JobExecutionResult> ScheduleAsync(string queue, WorkerMessage message, CancellationToken token)
    {
        await unitOfWork.BeginTransactionAsync(token);
        try
        {
            await unitOfWork.AcquireResourceLockAsync("worker-delivery", message.JobId, token);
            var job = await jobs.GetByIdAsync(message.JobId, token);
            if (job is null || job.ProjectId != message.ProjectId || job.RepositoryId != message.RepositoryId || job.QueueName != queue)
                return JobExecutionResult.DeadLetter();
            if (job.Status is JobStatus.Completed or JobStatus.Cancelled or JobStatus.Failed or JobStatus.DeadLettered or JobStatus.Timeout)
                return JobExecutionResult.Completed();
            var now = clock.UtcNow;
            var staleAfter = message is RepositoryAnalysisJobMessage ? TimeSpan.FromMinutes(30) : M2JobExecution.LeaseDuration;
            var next = job.Status == JobStatus.Running && job.LastHeartbeatAt is not null
                ? job.LastHeartbeatAt.Value.Add(staleAfter) : job.AvailableAt;
            if (next <= now)
            {
                unitOfWork.ClearTrackedChanges();
                var claimed = await jobs.TryClaimAsync(message.JobId, now, staleAfter, token);
                if (claimed is not null)
                {
                    if (claimed.AttemptCount >= claimed.MaxAttempts)
                    {
                        claimed.MarkDeadLettered("WORKER_DELIVERY_FAILED", "Consumer recovery retry budget exhausted.", now);
                        await unitOfWork.SaveChangesAsync(token); await unitOfWork.CommitTransactionAsync(token);
                        return JobExecutionResult.DeadLetter();
                    }
                    next = now.AddSeconds(Math.Min(300, 5 * Math.Pow(2, claimed.AttemptCount)));
                    claimed.MarkRetrying("WORKER_DELIVERY_FAILED", "Consumer recovery scheduled through durable outbox.", next, now);
                }
                else next = now.Add(staleAfter);
            }
            await outbox.EnqueueScheduledAsync(queue, message switch
            {
                RepositoryAnalysisJobMessage m => m with { MessageId = Guid.NewGuid(), AttemptCount = m.AttemptCount + 1 },
                HostedRepositoryProvisionJobMessage m => m with { MessageId = Guid.NewGuid(), AttemptCount = m.AttemptCount + 1 },
                HostedRepositoryReconcileJobMessage m => m with { MessageId = Guid.NewGuid(), AttemptCount = m.AttemptCount + 1 },
                RepositorySyncJobMessage m => m with { MessageId = Guid.NewGuid(), AttemptCount = m.AttemptCount + 1 },
                _ => throw new InvalidOperationException("Unsupported worker queue.")
            }, next, token);
            await unitOfWork.SaveChangesAsync(token); await unitOfWork.CommitTransactionAsync(token);
            return JobExecutionResult.Completed();
        }
        finally { await unitOfWork.RollbackTransactionAsync(CancellationToken.None); }
    }
}
