using System.Text.Json;
using GodForge.Application.Common.Constants;
using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models;
using GodForge.Application.Common.Models.Messages;
using GodForge.Application.Common.Security;
using GodForge.Domain.Entities.Ops;
using GodForge.Domain.Enums;
using MediatR;

namespace GodForge.Application.Features.Repositories.Commands.TriggerRepositoryAnalysis;

public sealed class TriggerRepositoryAnalysisCommandHandler : IRequestHandler<TriggerRepositoryAnalysisCommand, Result<Guid>>
{
    private readonly IGitRepositoryRepository _repositories;
    private readonly IJobRepository _jobs;
    private readonly IAuthorizationService _authorization;
    private readonly IOutboxWriter _outbox;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public TriggerRepositoryAnalysisCommandHandler(
        IGitRepositoryRepository repositories,
        IJobRepository jobs,
        IAuthorizationService authorization,
        IOutboxWriter outbox,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _repositories = repositories;
        _jobs = jobs;
        _authorization = authorization;
        _outbox = outbox;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<Guid>> Handle(
        TriggerRepositoryAnalysisCommand request,
        CancellationToken cancellationToken)
    {
        if (!await _authorization.HasPermissionAsync(
                request.ActorId,
                request.ProjectId,
                Permissions.AnalysisTrigger,
                cancellationToken))
        {
            return ApplicationError.Forbidden("SECURITY_FORBIDDEN", "You do not have permission to trigger repository analysis.");
        }

        var repository = await _repositories.GetByProjectIdAsync(request.ProjectId, cancellationToken);
        if (repository is null)
        {
            return ApplicationError.NotFound("REPOSITORY_NOT_CONNECTED", "No repository is connected to this project.");
        }

        var branch = string.IsNullOrWhiteSpace(request.Branch) ? repository.DefaultBranch : request.Branch.Trim();
        var now = _clock.UtcNow;

        // The remote HEAD is unknown until the worker fetches it. Reusing repository.CurrentCommitHash
        // here would incorrectly suppress a new job after the remote branch advances. The unique
        // trigger key deduplicates RabbitMQ redelivery through JobId while allowing each manual
        // analysis request to discover the latest remote revision.
        var triggerKey = $"manual:{repository.Id:N}:{Guid.NewGuid():N}";

        var payload = JsonSerializer.Serialize(new
        {
            repositoryId = repository.Id,
            branch,
            analysisProfile = request.AnalysisProfile,
            includeAi = request.IncludeAi
        });

        var job = Job.Create(
            request.ProjectId,
            repository.Id,
            JobType.AnalyzeProject,
            WorkerQueueNames.RepositoryPipeline,
            priority: 0,
            payload,
            triggerKey,
            maxAttempts: 3,
            request.ActorId,
            request.CorrelationId,
            availableAt: now,
            now);

        await _jobs.AddAsync(job, cancellationToken);

        var message = new RepositoryAnalysisJobMessage
        {
            JobId = job.Id,
            ProjectId = request.ProjectId,
            RepositoryId = repository.Id,
            CorrelationId = request.CorrelationId,
            InputHash = triggerKey,
            Branch = branch,
            AnalysisProfile = request.AnalysisProfile,
            IncludeAi = request.IncludeAi,
            CreatedAt = now
        };

        await _outbox.EnqueueAsync(WorkerQueueNames.RepositoryPipeline, message, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return job.Id;
    }
}
