using System.Security.Cryptography;
using System.Text;
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
    private readonly IJobPublisher _publisher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public TriggerRepositoryAnalysisCommandHandler(
        IGitRepositoryRepository repositories,
        IJobRepository jobs,
        IAuthorizationService authorization,
        IJobPublisher publisher,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _repositories = repositories;
        _jobs = jobs;
        _authorization = authorization;
        _publisher = publisher;
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
        var inputHash = CreateInputHash(repository.Id, branch, request.AnalysisProfile, request.IncludeAi, repository.CurrentCommitHash);
        var existingJob = await _jobs.GetByIdempotencyKeyAsync(
            request.ProjectId,
            JobType.AnalyzeProject,
            inputHash,
            cancellationToken);
        if (existingJob is not null)
        {
            return existingJob.Id;
        }

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
            inputHash,
            maxAttempts: 3,
            request.ActorId,
            request.CorrelationId,
            availableAt: now,
            now);

        await _jobs.AddAsync(job, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var message = new RepositoryAnalysisJobMessage
        {
            JobId = job.Id,
            ProjectId = request.ProjectId,
            RepositoryId = repository.Id,
            CorrelationId = request.CorrelationId,
            InputHash = inputHash,
            Branch = branch,
            AnalysisProfile = request.AnalysisProfile,
            IncludeAi = request.IncludeAi,
            CreatedAt = now
        };

        try
        {
            await _publisher.PublishAsync(WorkerQueueNames.RepositoryPipeline, message, cancellationToken);
        }
        catch (Exception)
        {
            job.MarkFailed("JOB_PUBLISH_FAILED", "The background job could not be published.", _clock.UtcNow);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return ApplicationError.Internal("JOB_PUBLISH_FAILED", "The analysis job could not be queued.");
        }

        return job.Id;
    }
    private static string CreateInputHash(
        Guid repositoryId,
        string branch,
        string profile,
        bool includeAi,
        string? currentCommitHash)
    {
        var input = string.Join(':', repositoryId, branch, profile, includeAi, currentCommitHash ?? "latest");
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input))).ToLowerInvariant();
    }

}
