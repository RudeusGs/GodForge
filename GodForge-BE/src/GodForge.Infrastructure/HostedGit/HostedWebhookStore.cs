using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Models;
using GodForge.Domain.Entities.Repo;
using GodForge.Domain.Enums;
using GodForge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GodForge.Infrastructure.HostedGit;

public sealed class HostedWebhookStore(GodForgeDbContext context) : IHostedWebhookStore
{
    public async Task<(GitRepository Repository, Guid OrganizationId)?> ResolveAsync(string providerId, CancellationToken cancellationToken)
    {
        var row = await (from r in context.GitRepositories
                         join p in context.Projects on r.ProjectId equals p.Id
                         join o in context.Organizations on p.OrganizationId equals o.Id
                         where r.Provider == GitProvider.Forgejo && r.Mode == RepositoryMode.InternalHosted && r.HostedRepositoryId == providerId &&
                             r.GitRepositoryStatus == GitRepositoryStatus.Ready && p.Status == ProjectStatus.Active && o.Status == OrganizationStatus.Active
                         select new { Repository = r, p.OrganizationId }).SingleOrDefaultAsync(cancellationToken);
        return row is null ? null : (row.Repository, row.OrganizationId);
    }
    public Task<bool> DeliveryExistsAsync(string deliveryId, CancellationToken cancellationToken)
        => context.WebhookEvents.AnyAsync(e => e.Provider == "Forgejo" && e.DeliveryId == deliveryId, cancellationToken);
    public async Task RecordAsync(WebhookEvent webhook, CancellationToken cancellationToken) => await context.WebhookEvents.AddAsync(webhook, cancellationToken);
    public async Task RecordSyncRunAsync(Guid repositoryId, DateTimeOffset started, DateTimeOffset completed, string? errorCode, CancellationToken token)
    {
        var run = RepositorySyncRun.Create(repositoryId, "metadata", started);
        if (errorCode is null) run.MarkAsCompleted(completed); else run.MarkAsFailed(errorCode, completed);
        await context.RepositorySyncRuns.AddAsync(run, token);
    }
    public async Task DeleteReferenceAsync(Guid repositoryId, string branch, CancellationToken cancellationToken)
    {
        var reference = await context.GitRefs.SingleOrDefaultAsync(r => r.RepositoryId == repositoryId && r.Type == "branch" && r.Name == branch, cancellationToken);
        if (reference is not null) context.GitRefs.Remove(reference);
    }
    public async Task SaveRevisionAsync(Guid repositoryId, string branch, RepositoryCommitDto commit, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (!await context.GitCommits.AnyAsync(c => c.RepositoryId == repositoryId && c.Hash == commit.Sha, cancellationToken))
            context.GitCommits.Add(GitCommit.Create(repositoryId, commit.Sha, commit.TreeSha, commit.Author, commit.AuthorEmail, commit.Message, commit.Timestamp.ToUniversalTime(), commit.Timestamp.ToUniversalTime(), now));
        var reference = await context.GitRefs.SingleOrDefaultAsync(r => r.RepositoryId == repositoryId && r.Type == "branch" && r.Name == branch, cancellationToken);
        var repository = await context.GitRepositories.SingleAsync(r => r.Id == repositoryId, cancellationToken);
        if (reference is null) context.GitRefs.Add(GitRef.Create(repositoryId, "branch", branch, commit.Sha, branch == repository.DefaultBranch, now));
        else reference.UpdateCommit(commit.Sha, now);
        if (branch == repository.DefaultBranch) repository.MarkSynchronized(commit.Sha, repository.RepoSizeBytes, now);
    }
}
