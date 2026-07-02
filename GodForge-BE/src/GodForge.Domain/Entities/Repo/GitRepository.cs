using GodForge.Domain.Common;
using GodForge.Domain.Enums;

namespace GodForge.Domain.Entities.Repo;

public sealed class GitRepository : BaseAuditableEntity, ISoftDeletable
{
    public Guid ProjectId { get; private set; }
    public string RemoteUrl { get; private set; } = default!;
    public GitProvider Provider { get; private set; }
    public string DefaultBranch { get; private set; } = default!;
    public string? WorkspaceKey { get; private set; }
    public GitRepositoryStatus GitRepositoryStatus { get; private set; }
    public DateTimeOffset? LastSyncedAt { get; private set; }
    public long? RepoSizeBytes { get; private set; }
    public string? CurrentCommitHash { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    private GitRepository() { }

    public static GitRepository Create(Guid projectId, string remoteUrl, GitProvider provider, string defaultBranch, DateTimeOffset now)
    {
        return new GitRepository
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            RemoteUrl = remoteUrl,
            Provider = provider,
            DefaultBranch = defaultBranch,
            GitRepositoryStatus = GitRepositoryStatus.Unconfigured,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateGitRepositoryStatus(GitRepositoryStatus newStatus, DateTimeOffset now)
    {
        GitRepositoryStatus = newStatus;
        UpdatedAt = now;
    }
    
    public void SoftDelete(DateTimeOffset now)
    {
        if (DeletedAt is not null) return;
        DeletedAt = now;
        GitRepositoryStatus = GitRepositoryStatus.Disabled;
        UpdatedAt = now;
    }
}
