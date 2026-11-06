using GodForge.Application.Common.Constants;
using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models;
using GodForge.Application.Common.Models.Analysis;
using GodForge.Application.Common.Models.Messages;
using GodForge.Application.Common.Security;
using GodForge.Domain.Enums;

namespace GodForge.Worker.Handlers;

public sealed class HostedRepositoryProvisionHandler(M2JobExecution execution, IGitRepositoryRepository repositories,
    IProjectRepository projects, IOrganizationRepository organizations, IAuthorizationService authorization,
    IHostedGitService provider, IClock clock)
{
    private sealed record Input(HostedRepositoryProvisionRequest Request, HostedRepositoryProvisionResult? Existing);
    private sealed record Output(HostedRepositoryProvisionResult Repository, long HookId);
    public Task<JobExecutionResult> HandleAsync(HostedRepositoryProvisionJobMessage message, CancellationToken cancellationToken)
        => execution.RunAsync<Input, Output>(message, JobType.ProvisionHostedRepository, WorkerQueueNames.HostedRepositoryProvision,
            async (job, token) =>
            {
                var repository = await repositories.GetByIdAsync(message.RepositoryId!.Value, token);
                if (repository is null || repository.ProjectId != message.ProjectId || !await EligibleAsync(job.TriggeredBy, message, token))
                    throw new HostedGitException("REPOSITORY_PERMISSION_DENIED");
                HostedRepositoryProvisionResult? existing = repository.HostedRepositoryId is null ? null :
                    new(repository.HostedRepositoryId, repository.CloneUrlSanitized, repository.CloneUrlSanitized[..^4], repository.DefaultBranch);
                if (existing is null) repository.BeginProvisioning(clock.UtcNow);
                return new(new("", $"{repository.HostedName}-{repository.Id:N}", true, repository.DefaultBranch, repository.ProvisionIdentity), existing);
            },
            async (input, token) =>
            {
                var result = input.Existing ?? await provider.ProvisionAsync(input.Request, token);
                return new(result, await provider.EnsureWebhookAsync(result.ProviderRepositoryId, null, token));
            },
            async (job, input, output, token) =>
            {
                var result = output.Repository;
                var repository = await repositories.GetByIdAsync(message.RepositoryId!.Value, token)
                    ?? throw new HostedGitException("REPOSITORY_NOT_CONNECTED");
                // Persist a recovered provider identity even if the project was archived during I/O.
                // It remains mapped and can be revoked/recovered rather than becoming orphaned.
                if (repository.HostedRepositoryId is null)
                    repository.CompleteProvisioning(result.ProviderRepositoryId, result.CloneUrl, clock.UtcNow);
                else if (repository.HostedRepositoryId != result.ProviderRepositoryId)
                    throw new HostedGitException("FORGEJO_IDENTITY_CONFLICT");
                if (!await EligibleAsync(job.TriggeredBy, message, token))
                {
                    repository.UpdateGitRepositoryStatus(GitRepositoryStatus.Disabled, clock.UtcNow);
                    await execution.QueueReconciliationAsync(repository.Id, message.ProjectId, message.OrganizationId, message.CorrelationId, token);
                    throw new HostedGitException("REPOSITORY_PERMISSION_DENIED");
                }
                repository.UpdateGitRepositoryStatus(GitRepositoryStatus.Ready, clock.UtcNow);
                repository.ConfirmWebhook(output.HookId, clock.UtcNow);
            },
            async (code, token) =>
            {
                var repository = await repositories.GetByIdAsync(message.RepositoryId!.Value, token);
                if (repository is { HostedRepositoryId: null }) repository.FailProvisioning(code, clock.UtcNow);
            }, cancellationToken);

    private async Task<bool> EligibleAsync(Guid? actor, HostedRepositoryProvisionJobMessage message, CancellationToken token)
    {
        var project = await projects.GetByIdAsync(message.ProjectId, token);
        var organization = await organizations.GetByIdAsync(message.OrganizationId, token);
        return project?.OrganizationId == message.OrganizationId && project.Status == ProjectStatus.Active &&
            organization?.Status == OrganizationStatus.Active && actor is not null &&
            await authorization.HasPermissionAsync(actor.Value, message.ProjectId, Permissions.RepositoryManage, token);
    }
}
