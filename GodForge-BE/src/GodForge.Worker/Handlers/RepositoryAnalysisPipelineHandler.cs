using System.Text.Json;
using GodForge.Application.Common.Constants;
using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models;
using GodForge.Application.Common.Models.Messages;
using GodForge.Application.Common.Text;
using GodForge.Domain.Entities.Ops;
using GodForge.Domain.Entities.Repo;
using GodForge.Domain.Enums;
using GodForge.Worker.Handlers.Stages;
using Microsoft.Extensions.DependencyInjection;

namespace GodForge.Worker.Handlers;

public sealed class RepositoryAnalysisPipelineHandler
{
    private static readonly TimeSpan ClaimStaleAfter = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan ClaimHeartbeatInterval = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan ShutdownRetryDelay = TimeSpan.FromSeconds(5);

    private readonly IJobRepository _jobs;
    private readonly IGitRepositoryRepository _repositories;
    private readonly IOutboxWriter _outbox;
    private readonly RepositoryDeterministicAnalysisStage _deterministicStage;
    private readonly RepositoryAnalysisPersistenceStage _persistenceStage;
    private readonly RepositoryAiAnalysisStage _aiStage;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IClock _clock;
    private readonly ILogger<RepositoryAnalysisPipelineHandler> _logger;

    public RepositoryAnalysisPipelineHandler(
        IJobRepository jobs,
        IGitRepositoryRepository repositories,
        IOutboxWriter outbox,
        RepositoryDeterministicAnalysisStage deterministicStage,
        RepositoryAnalysisPersistenceStage persistenceStage,
        RepositoryAiAnalysisStage aiStage,
        IUnitOfWork unitOfWork,
        IServiceScopeFactory scopeFactory,
        IClock clock,
        ILogger<RepositoryAnalysisPipelineHandler> logger)
    {
        _jobs = jobs;
        _repositories = repositories;
        _outbox = outbox;
        _deterministicStage = deterministicStage;
        _persistenceStage = persistenceStage;
        _aiStage = aiStage;
        _unitOfWork = unitOfWork;
        _scopeFactory = scopeFactory;
        _clock = clock;
        _logger = logger;
    }

    public async Task<JobExecutionResult> HandleAsync(
        RepositoryAnalysisJobMessage message,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var job = await _jobs.TryClaimAsync(message.JobId, now, ClaimStaleAfter, cancellationToken);
        if (job is null)
            return await HandleUnclaimedMessageAsync(message, now, cancellationToken);

        var claimToken = job.ClaimToken
            ?? throw new InvalidOperationException($"Claimed job '{job.Id}' does not have a claim token.");
        var claimedAttempt = job.AttemptCount;
        GitRepository? repository = null;

        using var executionCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var heartbeatTask = MaintainClaimHeartbeatAsync(
            job.Id,
            claimToken,
            executionCancellation,
            executionCancellation.Token);

        try
        {
            var executionToken = executionCancellation.Token;
            if (await _jobs.IsCancellationRequestedAsync(job.Id, executionToken))
                throw new JobCancellationRequestedException();

            if (message.RepositoryId is null)
            {
                job.MarkDeadLettered("WORKER_MESSAGE_INVALID", "RepositoryId is required.", _clock.UtcNow);
                await _unitOfWork.SaveChangesAsync(executionToken);
                return JobExecutionResult.DeadLetter();
            }

            repository = await _repositories.GetByIdAsync(message.RepositoryId.Value, executionToken);
            if (repository is null || repository.ProjectId != message.ProjectId)
            {
                job.MarkDeadLettered(
                    "REPOSITORY_NOT_CONNECTED",
                    "Repository was not found for this project.",
                    _clock.UtcNow);
                await _unitOfWork.SaveChangesAsync(executionToken);
                return JobExecutionResult.DeadLetter();
            }

            repository.MarkSyncStarted(repository.Id.ToString("N"), _clock.UtcNow);
            await _unitOfWork.SaveChangesAsync(executionToken);

            var deterministicResult = await _deterministicStage.ExecuteAsync(
                repository,
                message,
                (progress, token) => ReportProgressAsync(job, progress, token),
                executionToken);

            var analysisRun = await _persistenceStage.StageAsync(
                message,
                job,
                repository,
                deterministicResult,
                executionToken);

            await ReportProgressAsync(job, 85, executionToken);
            var aiResult = await _aiStage.StageAsync(
                message,
                repository,
                deterministicResult,
                executionToken);
            await ReportProgressAsync(job, 95, executionToken);

            repository.MarkSynchronized(
                deterministicResult.Sync.CommitSha,
                deterministicResult.Sync.RepositorySizeBytes,
                _clock.UtcNow);

            var result = JsonSerializer.Serialize(new
            {
                repositoryId = repository.Id,
                deterministicResult.Sync.CommitSha,
                deterministicResult.Sync.Branch,
                deterministic = deterministicResult.Deterministic,
                context = new
                {
                    deterministicResult.Context.InputHash,
                    deterministicResult.Context.IncludedFileCount,
                    deterministicResult.Context.SkippedFileCount,
                    deterministicResult.Context.WasTruncated,
                    deterministicResult.Context.Warnings
                },
                ai = new
                {
                    status = EnumText.ToSnakeCase(aiResult.Status),
                    summary = aiResult.Summary,
                    findingCount = aiResult.FindingCount,
                    errorCode = aiResult.ErrorCode
                }
            });

            analysisRun.MarkAsCompleted(_clock.UtcNow);
            job.MarkCompleted(result, _clock.UtcNow);
            await _unitOfWork.SaveChangesAsync(executionToken);
            return JobExecutionResult.Completed();
        }
        catch (JobCancellationRequestedException)
        {
            job.Cancel(_clock.UtcNow);
            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (ConcurrencyConflictException)
            {
                // A stale execution must not overwrite a newer worker claim.
                _unitOfWork.ClearTrackedChanges();
            }

            return JobExecutionResult.Completed();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await ScheduleRetryAfterShutdownAsync(message, claimToken, claimedAttempt);
            return JobExecutionResult.Completed();
        }
        catch (Exception exception)
        {
            return await HandleExecutionFailureAsync(
                message,
                repository?.Id,
                claimToken,
                claimedAttempt,
                exception,
                cancellationToken);
        }
        finally
        {
            await executionCancellation.CancelAsync();
            await heartbeatTask;
        }
    }

    private async Task<JobExecutionResult> HandleUnclaimedMessageAsync(
        RepositoryAnalysisJobMessage message,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var job = await _jobs.GetByIdAsync(message.JobId, cancellationToken);
        if (job is null)
        {
            _logger.LogWarning("Job {JobId} does not exist; message will be dead-lettered", message.JobId);
            return JobExecutionResult.DeadLetter();
        }

        if (IsTerminal(job.Status))
            return JobExecutionResult.Completed();

        if (job.CancellationRequestedAt is not null)
        {
            job.Cancel(now);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return JobExecutionResult.Completed();
        }

        if (job.Status == JobStatus.Running)
        {
            // RabbitMQ may redeliver after the original worker dies while the database
            // claim is still fresh. Persist a watchdog delivery for the stale boundary
            // before acknowledging this copy, otherwise the job could remain Running
            // forever with no message left to reclaim it.
            var heartbeatAt = job.LastHeartbeatAt ?? job.StartedAt ?? now;
            var recoveryAt = heartbeatAt.Add(ClaimStaleAfter);
            if (recoveryAt <= now)
                recoveryAt = now.AddSeconds(1);

            await EnqueueRetryAsync(job, message, recoveryAt, now, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Scheduled claim recovery for running job {JobId} at {RecoveryAt}",
                job.Id,
                recoveryAt);
        }

        // Retrying jobs already have a durable outbox row committed atomically with
        // their state. Queued jobs retain their original delivery. This message is a
        // duplicate and can be acknowledged safely.
        return JobExecutionResult.Completed();
    }

    private async Task ReportProgressAsync(Job job, int progress, CancellationToken cancellationToken)
    {
        job.UpdateProgress(progress, _clock.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (await _jobs.IsCancellationRequestedAsync(job.Id, cancellationToken))
            throw new JobCancellationRequestedException();
    }

    private async Task<JobExecutionResult> HandleExecutionFailureAsync(
        RepositoryAnalysisJobMessage message,
        Guid? repositoryId,
        Guid claimToken,
        int claimedAttempt,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(
            exception,
            "Repository analysis job {JobId} failed at attempt {AttemptCount}",
            message.JobId,
            claimedAttempt);

        // SaveChanges failures leave pending inserts and stale concurrency snapshots in
        // the context. Clear them before writing retry/dead-letter state.
        _unitOfWork.ClearTrackedChanges();

        var failedJob = await _jobs.GetByIdAsync(message.JobId, cancellationToken);
        if (failedJob is null)
            return JobExecutionResult.DeadLetter();

        // A different worker reclaimed the stale lease, or already completed the job.
        // This delivery must not overwrite the newer execution state.
        if (IsTerminal(failedJob.Status) ||
            failedJob.Status != JobStatus.Running ||
            failedJob.ClaimToken != claimToken ||
            failedJob.AttemptCount != claimedAttempt)
        {
            return JobExecutionResult.Completed();
        }

        GitRepository? failedRepository = null;
        if (repositoryId is not null)
            failedRepository = await _repositories.GetByIdAsync(repositoryId.Value, cancellationToken);

        failedRepository?.MarkError("REPOSITORY_ANALYSIS_FAILED", _clock.UtcNow);

        if (failedJob.AttemptCount < failedJob.MaxAttempts && IsRetryable(exception))
        {
            var now = _clock.UtcNow;
            var delay = CalculateRetryDelay(failedJob.AttemptCount);
            var availableAt = now.Add(delay);
            failedJob.MarkRetrying(
                "JOB_TRANSIENT_FAILURE",
                "A transient dependency failed. The job will be retried.",
                availableAt,
                now);

            await EnqueueRetryAsync(failedJob, message, availableAt, now, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return JobExecutionResult.Completed();
        }

        failedJob.MarkDeadLettered(
            "REPOSITORY_ANALYSIS_FAILED",
            "Repository analysis failed after the allowed attempts.",
            _clock.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return JobExecutionResult.DeadLetter();
    }

    private async Task ScheduleRetryAfterShutdownAsync(
        RepositoryAnalysisJobMessage message,
        Guid claimToken,
        int claimedAttempt)
    {
        _unitOfWork.ClearTrackedChanges();
        var job = await _jobs.GetByIdAsync(message.JobId, CancellationToken.None);
        if (job is null ||
            job.Status != JobStatus.Running ||
            job.ClaimToken != claimToken ||
            job.AttemptCount != claimedAttempt)
        {
            return;
        }

        var now = _clock.UtcNow;
        var availableAt = now.Add(ShutdownRetryDelay);
        job.MarkRetrying(
            "WORKER_SHUTDOWN",
            "The worker stopped before the job completed.",
            availableAt,
            now);
        await EnqueueRetryAsync(job, message, availableAt, now, CancellationToken.None);
        await _unitOfWork.SaveChangesAsync(CancellationToken.None);
    }

    private Task EnqueueRetryAsync(
        Job job,
        RepositoryAnalysisJobMessage message,
        DateTimeOffset availableAt,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var retryMessage = message with
        {
            MessageId = Guid.NewGuid(),
            AttemptCount = job.AttemptCount,
            CreatedAt = now
        };

        return _outbox.EnqueueScheduledAsync(
            string.IsNullOrWhiteSpace(job.QueueName)
                ? WorkerQueueNames.RepositoryPipeline
                : job.QueueName,
            retryMessage,
            availableAt,
            cancellationToken);
    }

    private async Task MaintainClaimHeartbeatAsync(
        Guid jobId,
        Guid claimToken,
        CancellationTokenSource executionCancellation,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(ClaimHeartbeatInterval, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var jobs = scope.ServiceProvider.GetRequiredService<IJobRepository>();
                var renewed = await jobs.TryHeartbeatAsync(
                    jobId,
                    claimToken,
                    _clock.UtcNow,
                    cancellationToken);

                if (renewed)
                    continue;

                _logger.LogWarning(
                    "Repository analysis job {JobId} lost claim {ClaimToken}; cancelling the stale execution",
                    jobId,
                    claimToken);
                executionCancellation.Cancel();
                return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Repository analysis job {JobId} could not renew claim {ClaimToken}; cancelling the execution",
                    jobId,
                    claimToken);
                executionCancellation.Cancel();
                return;
            }
        }
    }

    private static TimeSpan CalculateRetryDelay(int attemptCount)
        => TimeSpan.FromSeconds(Math.Min(60, Math.Pow(2, attemptCount) * 5));

    private static bool IsTerminal(JobStatus status)
        => status is JobStatus.Completed or JobStatus.Cancelled or JobStatus.Failed or JobStatus.Timeout or JobStatus.DeadLettered;

    private static bool IsRetryable(Exception exception)
        => exception is IOException or HttpRequestException or TimeoutException or TaskCanceledException or OperationCanceledException or ConcurrencyConflictException ||
           exception.InnerException is IOException or HttpRequestException;

    private sealed class JobCancellationRequestedException : Exception { }
}
