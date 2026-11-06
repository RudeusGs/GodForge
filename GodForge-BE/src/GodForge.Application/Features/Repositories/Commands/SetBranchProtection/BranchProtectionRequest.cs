using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models;
using GodForge.Application.Common.Security;
using MediatR;

namespace GodForge.Application.Features.Repositories.Commands.SetBranchProtection;

public sealed record BranchProtectionRequest(Guid ProjectId, Guid ActorId, string Branch, bool? Enabled) : IRequest<Result<object>>;
public sealed class BranchProtectionHandler(IGitRepositoryRepository repositories, IAuthorizationService authorization, IHostedGitService provider,
    IProjectRepository projects, IUnitOfWork unitOfWork, IAuditWriter audit, IRepositoryLockProvider repositoryLocks)
    : IRequestHandler<BranchProtectionRequest, Result<object>>
{
    public async Task<Result<object>> Handle(BranchProtectionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (!await authorization.HasPermissionAsync(request.ActorId, request.ProjectId, Permissions.RepositoryRead, cancellationToken))
                return ApplicationError.NotFound("REPOSITORY_NOT_CONNECTED", "Repository was not found.");
            if (request.Enabled is not null && !await authorization.HasPermissionAsync(request.ActorId, request.ProjectId, Permissions.RepositoryManage, cancellationToken))
                return ApplicationError.Forbidden("REPOSITORY_PERMISSION_DENIED", "You cannot configure branch protection.");
            var repository = await repositories.GetByProjectIdAsync(request.ProjectId, cancellationToken);
            if (repository?.HostedRepositoryId is null) return ApplicationError.Conflict("REPOSITORY_NOT_CONNECTED", "A provisioned hosted repository is required.");
            try
            {
                await using var lease = await repositoryLocks.AcquireOperationAsync(repository.Id, TimeSpan.FromSeconds(10), cancellationToken);
                var enabled = await provider.ProtectionAsync(repository.HostedRepositoryId, request.Branch, request.Enabled, cancellationToken);
                if (request.Enabled is not null)
                {
                    await unitOfWork.BeginTransactionAsync(cancellationToken);
                    var project = await projects.GetByIdAsync(request.ProjectId, cancellationToken);
                    if (project is null) return ApplicationError.NotFound("PROJECT_NOT_FOUND", "Project was not found.");
                    await unitOfWork.AcquireResourceLockAsync("organization-membership", project.OrganizationId, cancellationToken);
                    await unitOfWork.AcquireResourceLockAsync("project-membership", request.ProjectId, cancellationToken);
                    unitOfWork.ClearTrackedChanges();
                    if (!await authorization.HasPermissionAsync(request.ActorId, request.ProjectId, Permissions.RepositoryManage, cancellationToken))
                        return ApplicationError.Forbidden("REPOSITORY_PERMISSION_DENIED", "Repository access changed during the operation.");
                    await audit.WriteAuditAsync(request.ActorId, request.ProjectId, "repository.branch.protection.updated", "repository", repository.Id,
                        "success", new { branch = request.Branch, enabled }, cancellationToken);
                    await unitOfWork.SaveChangesAsync(cancellationToken);
                    await unitOfWork.CommitTransactionAsync(cancellationToken);
                }
                return Result<object>.Success(new { branch = request.Branch, enabled });
            }
            catch (HostedGitException e) { return ApplicationError.Conflict(e.Code, "Branch protection could not be confirmed."); }
        }
        finally { if (request.Enabled is not null) await unitOfWork.RollbackTransactionAsync(CancellationToken.None); }
    }
}
