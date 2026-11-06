using GodForge.Application.Common.Constants;
using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models;
using GodForge.Application.Common.Models.Messages;
using GodForge.Application.Common.Security;
using GodForge.Domain.Entities.Ops;
using GodForge.Domain.Enums;
using MediatR;

namespace GodForge.Application.Features.Repositories.Commands.SynchronizeRepository;

public sealed record SynchronizeRepositoryCommand(Guid ProjectId, Guid ActorId, string CorrelationId) : IRequest<Result<Guid>>;
public sealed class SynchronizeRepositoryCommandHandler(IGitRepositoryRepository repositories, IProjectRepository projects,
    IAuthorizationService authorization, IJobRepository jobs, IOutboxWriter outbox, IUnitOfWork unitOfWork, IClock clock)
    : IRequestHandler<SynchronizeRepositoryCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(SynchronizeRepositoryCommand request, CancellationToken token)
    {
        await unitOfWork.BeginTransactionAsync(token);
        try
        {
            var project = await projects.GetByIdAsync(request.ProjectId, token);
            if (project is not null)
            {
                await unitOfWork.AcquireResourceLockAsync("organization-membership", project.OrganizationId, token);
                await unitOfWork.AcquireResourceLockAsync("project-membership", request.ProjectId, token);
            }
            unitOfWork.ClearTrackedChanges();
            if (!await authorization.HasPermissionAsync(request.ActorId, request.ProjectId, Permissions.RepositoryRead, token))
                return ApplicationError.NotFound("REPOSITORY_NOT_CONNECTED", "Repository was not found.");
            if (!await authorization.HasPermissionAsync(request.ActorId, request.ProjectId, Permissions.RepositorySync, token))
                return ApplicationError.Forbidden("REPOSITORY_PERMISSION_DENIED", "Repository synchronization is not authorized.");
            var repository = await repositories.GetByProjectIdAsync(request.ProjectId, token);
            if (repository is null || !RepositoryInput.ValidBranch(repository.DefaultBranch))
                return ApplicationError.Conflict("REPOSITORY_NOT_CONNECTED", "A repository with a valid branch is required.");
            var now = clock.UtcNow;
            var key = $"sync:{Guid.NewGuid():N}";
            var job = Job.Create(request.ProjectId, repository.Id, JobType.FetchRepository, WorkerQueueNames.RepositorySync, 0, null, key,
                3, request.ActorId, request.CorrelationId, now, now);
            await jobs.AddAsync(job, token);
            await outbox.EnqueueAsync(job.QueueName, new RepositorySyncJobMessage
            {
                JobId = job.Id,
                RepositoryId = repository.Id,
                OrganizationId = project!.OrganizationId,
                ProjectId = request.ProjectId,
                Branch = repository.DefaultBranch,
                InputHash = key,
                CorrelationId = request.CorrelationId,
                CreatedAt = now
            }, token);
            await unitOfWork.SaveChangesAsync(token); await unitOfWork.CommitTransactionAsync(token);
            return job.Id;
        }
        finally { await unitOfWork.RollbackTransactionAsync(CancellationToken.None); }
    }
}
