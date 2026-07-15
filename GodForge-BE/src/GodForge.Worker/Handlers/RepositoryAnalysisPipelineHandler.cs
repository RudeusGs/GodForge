using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models.Messages;
using GodForge.Domain.Entities.Analysis;
using GodForge.Domain.Entities.Repo;
using GodForge.Domain.Enums;

namespace GodForge.Worker.Handlers;

public sealed class RepositoryAnalysisPipelineHandler
{
    private const string PromptVersion = "health-overview-v1";
    private readonly IJobRepository _jobs;
    private readonly IGitRepositoryRepository _repositories;
    private readonly IRepositorySnapshotRepository _snapshots;
    private readonly IAiAnalysisRepository _aiRepository;
    private readonly IRepositoryWorkspaceService _workspaceService;
    private readonly IDeterministicProjectAnalyzer _deterministicAnalyzer;
    private readonly IRepositoryContextBuilder _contextBuilder;
    private readonly IAiAnalysisProvider _aiProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ILogger<RepositoryAnalysisPipelineHandler> _logger;

    public RepositoryAnalysisPipelineHandler(
        IJobRepository jobs,
        IGitRepositoryRepository repositories,
        IRepositorySnapshotRepository snapshots,
        IAiAnalysisRepository aiRepository,
        IRepositoryWorkspaceService workspaceService,
        IDeterministicProjectAnalyzer deterministicAnalyzer,
        IRepositoryContextBuilder contextBuilder,
        IAiAnalysisProvider aiProvider,
        IUnitOfWork unitOfWork,
        IClock clock,
        ILogger<RepositoryAnalysisPipelineHandler> logger)
    {
        _jobs = jobs;
        _repositories = repositories;
        _snapshots = snapshots;
        _aiRepository = aiRepository;
        _workspaceService = workspaceService;
        _deterministicAnalyzer = deterministicAnalyzer;
        _contextBuilder = contextBuilder;
        _aiProvider = aiProvider;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _logger = logger;
    }

    public async Task<JobExecutionResult> HandleAsync(
        RepositoryAnalysisJobMessage message,
        CancellationToken cancellationToken)
    {
        var job = await _jobs.GetByIdAsync(message.JobId, cancellationToken);
        if (job is null)
        {
            _logger.LogWarning("Job {JobId} does not exist; message will be dead-lettered", message.JobId);
            return JobExecutionResult.DeadLetter();
        }

        if (job.Status is JobStatus.Completed or JobStatus.Cancelled or JobStatus.Failed or JobStatus.Timeout or JobStatus.DeadLettered)
        {
            return JobExecutionResult.Completed();
        }

        if (job.CancellationRequestedAt is not null)
        {
            job.Cancel(_clock.UtcNow);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return JobExecutionResult.Completed();
        }

        if (job.Status == JobStatus.Retrying && job.AvailableAt > _clock.UtcNow)
        {
            return JobExecutionResult.Retry(job.AvailableAt - _clock.UtcNow);
        }

        if (message.RepositoryId is null)
        {
            job.MarkDeadLettered("WORKER_MESSAGE_INVALID", "RepositoryId is required.", _clock.UtcNow);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return JobExecutionResult.DeadLetter();
        }

        var repository = await _repositories.GetByIdAsync(message.RepositoryId.Value, cancellationToken);
        if (repository is null || repository.ProjectId != message.ProjectId)
        {
            job.MarkDeadLettered("REPOSITORY_NOT_CONNECTED", "Repository was not found for this project.", _clock.UtcNow);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return JobExecutionResult.DeadLetter();
        }

        try
        {
            var now = _clock.UtcNow;
            job.MarkRunning(now);
            repository.MarkSyncStarted(repository.Id.ToString("N"), now);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var sync = await _workspaceService.SyncAsync(
                repository.Id,
                repository.RemoteUrl,
                message.Branch,
                cancellationToken);
            job.UpdateProgress(30, _clock.UtcNow);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (job.CancellationRequestedAt is not null)
            {
                job.Cancel(_clock.UtcNow);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return JobExecutionResult.Completed();
            }

            var deterministic = await _deterministicAnalyzer.AnalyzeAsync(sync.WorkspacePath, cancellationToken);
            job.UpdateProgress(55, _clock.UtcNow);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (job.CancellationRequestedAt is not null)
            {
                job.Cancel(_clock.UtcNow);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return JobExecutionResult.Completed();
            }

            var context = await _contextBuilder.BuildAsync(sync.WorkspacePath, cancellationToken);
            job.UpdateProgress(75, _clock.UtcNow);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (job.CancellationRequestedAt is not null)
            {
                job.Cancel(_clock.UtcNow);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return JobExecutionResult.Completed();
            }

            var snapshot = await _snapshots.GetByCommitAsync(repository.Id, sync.CommitSha, cancellationToken);
            if (snapshot is null)
            {
                snapshot = RepositorySnapshot.Create(repository.Id, sync.CommitSha, sync.Branch, _clock.UtcNow);
                await _snapshots.AddAsync(snapshot, cancellationToken);
            }

            snapshot.MarkAsReady(JsonSerializer.Serialize(new
            {
                deterministic,
                context = new
                {
                    context.InputHash,
                    context.IncludedFileCount,
                    context.SkippedFileCount,
                    context.WasTruncated,
                    context.Warnings
                }
            }));

            repository.MarkSynchronized(sync.CommitSha, sync.RepositorySizeBytes, _clock.UtcNow);

            string aiStatus = "not_requested";
            string? aiSummary = null;
            int aiFindingCount = 0;
            string? aiErrorCode = null;

            if (message.IncludeAi)
            {
                var cachedRun = await _aiRepository.GetCompletedAsync(
                    repository.Id,
                    sync.CommitSha,
                    message.AnalysisProfile,
                    _aiProvider.ProviderName,
                    _aiProvider.ModelName,
                    PromptVersion,
                    context.InputHash,
                    cancellationToken);

                if (cachedRun is not null)
                {
                    aiStatus = "cached";
                    aiSummary = cachedRun.Summary;
                }
                else
                {
                    var aiResult = await _aiProvider.AnalyzeAsync(new(
                        message.ProjectId,
                        repository.Id,
                        sync.CommitSha,
                        message.AnalysisProfile,
                        PromptVersion,
                        context,
                        deterministic), cancellationToken);

                    if (!aiResult.IsEnabled)
                    {
                        aiStatus = "disabled";
                    }
                    else
                    {
                        var run = AiAnalysisRun.Create(
                            message.ProjectId,
                            repository.Id,
                            sync.CommitSha,
                            message.AnalysisProfile,
                            _aiProvider.ProviderName,
                            _aiProvider.ModelName,
                            PromptVersion,
                            context.InputHash,
                            _clock.UtcNow);
                        await _aiRepository.AddRunAsync(run, cancellationToken);

                        if (aiResult.IsSuccess)
                        {
                            run.MarkCompleted(
                                aiResult.Summary ?? string.Empty,
                                aiResult.InputTokenCount,
                                aiResult.OutputTokenCount,
                                null,
                                null,
                                _clock.UtcNow);

                            var fingerprints = new HashSet<string>(StringComparer.Ordinal);
                            foreach (var finding in aiResult.Findings)
                            {
                                var evidenceJson = JsonSerializer.Serialize(finding.EvidenceRefs);
                                var fingerprint = CreateFingerprint(finding.Title, evidenceJson);
                                if (!fingerprints.Add(fingerprint))
                                {
                                    continue;
                                }

                                await _aiRepository.AddFindingAsync(AiFinding.Create(
                                    run.Id,
                                    finding.Category,
                                    finding.Severity,
                                    finding.Title,
                                    finding.Description,
                                    finding.Recommendation,
                                    finding.Confidence,
                                    evidenceJson,
                                    fingerprint,
                                    _clock.UtcNow), cancellationToken);
                            }

                            aiStatus = "completed";
                            aiSummary = aiResult.Summary;
                            aiFindingCount = fingerprints.Count;
                        }
                        else
                        {
                            aiErrorCode = aiResult.ErrorCode ?? "AI_ANALYSIS_FAILED";
                            run.MarkFailed(aiErrorCode, _clock.UtcNow);
                            aiStatus = "failed";
                        }
                    }
                }
            }

            var result = JsonSerializer.Serialize(new
            {
                repositoryId = repository.Id,
                sync.CommitSha,
                sync.Branch,
                deterministic,
                context = new
                {
                    context.InputHash,
                    context.IncludedFileCount,
                    context.SkippedFileCount,
                    context.WasTruncated,
                    context.Warnings
                },
                ai = new
                {
                    status = aiStatus,
                    summary = aiSummary,
                    findingCount = aiFindingCount,
                    errorCode = aiErrorCode
                }
            });

            job.MarkCompleted(result, _clock.UtcNow);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return JobExecutionResult.Completed();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Repository analysis job {JobId} failed at attempt {AttemptCount}",
                job.Id,
                job.AttemptCount);

            repository.MarkError("REPOSITORY_ANALYSIS_FAILED", _clock.UtcNow);
            if (job.AttemptCount < job.MaxAttempts && IsRetryable(exception))
            {
                var delay = TimeSpan.FromSeconds(Math.Min(60, Math.Pow(2, job.AttemptCount) * 5));
                job.MarkRetrying(
                    "JOB_TRANSIENT_FAILURE",
                    "A transient dependency failed. The job will be retried.",
                    _clock.UtcNow.Add(delay),
                    _clock.UtcNow);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return JobExecutionResult.Retry(delay);
            }

            job.MarkDeadLettered(
                "REPOSITORY_ANALYSIS_FAILED",
                "Repository analysis failed after the allowed attempts.",
                _clock.UtcNow);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return JobExecutionResult.DeadLetter();
        }
    }

    private static bool IsRetryable(Exception exception)
        => exception is IOException or HttpRequestException or TimeoutException or TaskCanceledException or OperationCanceledException ||
           exception.InnerException is IOException or HttpRequestException;

    private static string CreateFingerprint(string title, string evidenceJson)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(title + "\n" + evidenceJson));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

