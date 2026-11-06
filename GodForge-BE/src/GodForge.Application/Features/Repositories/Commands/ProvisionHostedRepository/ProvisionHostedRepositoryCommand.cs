using System.Text.Json;
using FluentValidation;
using GodForge.Application.Common.Constants;
using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models;
using GodForge.Application.Common.Models.Messages;
using GodForge.Application.Common.Security;
using GodForge.Domain.Entities.Ops;
using GodForge.Domain.Entities.Repo;
using GodForge.Domain.Enums;
using MediatR;

namespace GodForge.Application.Features.Repositories.Commands.ProvisionHostedRepository;

public sealed record ProvisionHostedRepositoryCommand(Guid ProjectId, string Name, string DefaultBranch,
    bool IsPrivate, bool AutoAnalyzeEnabled, Guid ActorId, string CorrelationId) : IRequest<Result<Guid>>;

public sealed class ProvisionHostedRepositoryCommandValidator : AbstractValidator<ProvisionHostedRepositoryCommand>
{
    public ProvisionHostedRepositoryCommandValidator()
    {
        RuleFor(r => r.ProjectId).NotEmpty();
        RuleFor(r => r.Name).Must(RepositoryInput.ValidName).WithMessage("Name must contain 1–60 lowercase letters, digits and interior hyphens.");
        RuleFor(r => r.DefaultBranch).Must(RepositoryInput.ValidBranch).WithMessage("A valid branch name is required.");
        RuleFor(r => r.IsPrivate).Equal(true).WithMessage("Hosted repositories must be private.");
    }
}

public sealed class ProvisionHostedRepositoryCommandHandler(
    IProjectRepository projects, IOrganizationRepository organizations, IGitRepositoryRepository repositories,
    IJobRepository jobs, IAuthorizationService authorization, IHostedGitService provider,
    IUnitOfWork unitOfWork, IOutboxWriter outbox, IActivityWriter activity, IClock clock)
    : IRequestHandler<ProvisionHostedRepositoryCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(ProvisionHostedRepositoryCommand request, CancellationToken cancellationToken)
    {
        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var project = await projects.GetByIdAsync(request.ProjectId, cancellationToken);
            if (project is not null)
            {
                await unitOfWork.AcquireResourceLockAsync("organization-membership", project.OrganizationId, cancellationToken);
                await unitOfWork.AcquireResourceLockAsync("organization-project-catalog", project.OrganizationId, cancellationToken);
                await unitOfWork.AcquireResourceLockAsync("project-membership", request.ProjectId, cancellationToken);
            }
            unitOfWork.ClearTrackedChanges();
            if (!await authorization.HasPermissionAsync(request.ActorId, request.ProjectId, Permissions.RepositoryRead, cancellationToken))
                return ApplicationError.NotFound("REPOSITORY_NOT_CONNECTED", "Repository was not found.");
            if (!await authorization.HasPermissionAsync(request.ActorId, request.ProjectId, Permissions.RepositoryManage, cancellationToken))
                return ApplicationError.Forbidden("REPOSITORY_PERMISSION_DENIED", "You cannot configure this repository.");
            project = await projects.GetByIdAsync(request.ProjectId, cancellationToken);
            var organization = await organizations.GetByIdAsync(project!.OrganizationId, cancellationToken);
            if (project.Status != ProjectStatus.Active || organization?.Status != OrganizationStatus.Active)
                return ApplicationError.Conflict("PROJECT_NOT_ACTIVE", "An active project and organization are required.");
            if (!provider.IsConfigured) return ApplicationError.Conflict("FORGEJO_NOT_CONFIGURED", "Hosted Git is not configured.");
            var repository = await repositories.GetByProjectIdAsync(request.ProjectId, cancellationToken);
            var now = clock.UtcNow;
            Job job;
            if (repository is not null)
            {
                if (repository.Mode != RepositoryMode.InternalHosted || repository.HostedName != request.Name ||
                    repository.DefaultBranch != request.DefaultBranch || repository.AutoAnalyzeEnabled != request.AutoAnalyzeEnabled)
                    return ApplicationError.Conflict("REPOSITORY_ALREADY_CONNECTED", "A repository is already configured for this project.");
                job = (await jobs.GetByIdempotencyKeyAsync(request.ProjectId, JobType.ProvisionHostedRepository, $"hosted:{repository.Id:N}", cancellationToken))!;
                if (job is null) return ApplicationError.Conflict("REPOSITORY_ALREADY_CONNECTED", "Repository was created outside this provisioning operation.");
                if (job.Status is not (JobStatus.Failed or JobStatus.DeadLettered)) return job.Id;
                job.RetryHostedProvision(now, request.ActorId);
            }
            else
            {
                repository = GitRepository.RequestHosted(request.ProjectId, request.Name, request.DefaultBranch, request.AutoAnalyzeEnabled, now);
                await repositories.AddAsync(repository, cancellationToken);
                job = Job.Create(request.ProjectId, repository.Id, JobType.ProvisionHostedRepository,
                    WorkerQueueNames.HostedRepositoryProvision, 0, null, $"hosted:{repository.Id:N}", 3,
                    request.ActorId, request.CorrelationId, now, now);
                await jobs.AddAsync(job, cancellationToken);
            }
            await outbox.EnqueueAsync(WorkerQueueNames.HostedRepositoryProvision, new HostedRepositoryProvisionJobMessage
            {
                JobId = job.Id,
                ProjectId = request.ProjectId,
                RepositoryId = repository.Id,
                OrganizationId = project.OrganizationId,
                CorrelationId = request.CorrelationId,
                InputHash = job.IdempotencyKey,
                CreatedAt = now
            }, cancellationToken);
            await activity.WriteAsync(request.ProjectId, request.ActorId, "repository.provision.requested", "repository",
                repository.Id.ToString(), ActivityStatus.Succeeded, request.CorrelationId, null, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);
            return job.Id;
        }
        finally { await unitOfWork.RollbackTransactionAsync(CancellationToken.None); }
    }
}
