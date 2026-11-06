using GodForge.Application.Common.Constants;
using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models;
using GodForge.Application.Common.Models.Analysis;
using GodForge.Application.Common.Models.Messages;
using GodForge.Application.Common.Security;
using GodForge.Domain.Entities.Repo;
using GodForge.Domain.Enums;

namespace GodForge.Worker.Handlers;

public sealed class RepositorySyncHandler(M2JobExecution execution, IGitRepositoryRepository repositories, IProjectRepository projects,
    IOrganizationRepository organizations, IAuthorizationService authorization, IHostedGitService provider, ILinkedRepositoryBrowser linked,
    IHostedWebhookStore revisions, IRepositorySnapshotRepository snapshots, IClock clock)
{
    private sealed record Input(string? ProviderId, string Remote, RepositoryMode Mode, long Generation);
    private sealed record Output(RepositoryCommitDto? Commit, bool Deleted, WorkspaceSyncResult? Workspace = null);
    public Task<JobExecutionResult> HandleAsync(RepositorySyncJobMessage message, CancellationToken cancellationToken)
        => execution.RunAsync<Input, Output>(message, JobType.FetchRepository, WorkerQueueNames.RepositorySync,
            async (job, token) =>
            {
                var repository = await ReadAsync(message, job.TriggeredBy, token);
                repository.BeginMetadataSync();
                return new(repository.HostedRepositoryId, repository.RemoteUrl, repository.Mode, repository.SyncGeneration);
            },
            async (input, token) =>
            {
                if (input.Mode == RepositoryMode.ExternalLinked)
                {
                    try
                    {
                        var workspace = await linked.SynchronizeAsync(message.RepositoryId!.Value, input.Remote, message.Branch, token);
                        var commits = await linked.CommitsAsync(message.RepositoryId.Value, message.Branch, 1, 1, token);
                        return new(commits.FirstOrDefault(), false, workspace);
                    }
                    catch (Exception e) when (e is ArgumentException or InvalidOperationException)
                    { throw new HostedGitException("REPOSITORY_SYNC_FAILED"); }
                }
                try { var commits = await provider.CommitsAsync(input.ProviderId!, message.Branch, 1, 1, token); return new(commits.FirstOrDefault(), false); }
                catch (HostedGitException e) when (message.IsDeletion && e.Code == "REPOSITORY_PROVIDER_NOT_FOUND") { return new(null, true); }
            },
            async (job, input, result, token) =>
            {
                var repository = await ReadAsync(message, job.TriggeredBy, token);
                if (repository.SyncGeneration != input.Generation) return; // Newer sync owns the mutable ref projection.
                await revisions.RecordSyncRunAsync(repository.Id, job.StartedAt ?? clock.UtcNow, clock.UtcNow, null, token);
                if (result.Deleted) await revisions.DeleteReferenceAsync(repository.Id, message.Branch, token);
                else if (result.Commit is not null)
                {
                    await revisions.SaveRevisionAsync(repository.Id, message.Branch, result.Commit, clock.UtcNow, token);
                    if (await snapshots.GetByCommitAsync(repository.Id, result.Commit.Sha, token) is null)
                    {
                        var snapshot = RepositorySnapshot.Create(repository.Id, result.Commit.Sha, message.Branch, clock.UtcNow);
                        snapshot.MarkAsReady(null); await snapshots.AddAsync(snapshot, token);
                    }
                    if (result.Workspace is not null) repository.MarkSynchronized(result.Commit.Sha, result.Workspace.RepositorySizeBytes, clock.UtcNow);
                }
            },
            async (code, token) =>
            {
                var repository = await repositories.GetByIdAsync(message.RepositoryId!.Value, token);
                if (repository?.ProjectId == message.ProjectId) repository.MarkError(code, clock.UtcNow);
            }, cancellationToken);

    private async Task<GitRepository> ReadAsync(RepositorySyncJobMessage message, Guid? actor, CancellationToken token)
    {
        var repository = await repositories.GetByIdAsync(message.RepositoryId!.Value, token);
        var project = await projects.GetByIdAsync(message.ProjectId, token);
        var organization = await organizations.GetByIdAsync(message.OrganizationId, token);
        if (repository is null || repository.ProjectId != message.ProjectId || project?.OrganizationId != message.OrganizationId ||
            project.Status != ProjectStatus.Active || organization?.Status != OrganizationStatus.Active ||
            repository.Mode == RepositoryMode.InternalHosted && repository.HostedRepositoryId is null ||
            actor is not null && !await authorization.HasPermissionAsync(actor.Value, message.ProjectId, Permissions.RepositorySync, token))
            throw new HostedGitException("REPOSITORY_NOT_CONNECTED");
        return repository;
    }
}
