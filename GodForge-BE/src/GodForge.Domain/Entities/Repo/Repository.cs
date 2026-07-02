using GodForge.Domain.Common;
using GodForge.Domain.Enums;

namespace GodForge.Domain.Entities.Repo;

public sealed class Repository : BaseAuditableEntity, ISoftDeletable
{
    public Guid ProjectId { get; private set; }
    public string RemoteUrl { get; private set; } = default!;
    public GitProvider Provider { get; private set; }
    public string DefaultBranch { get; private set; } = default!;
    public string? WorkspaceKey { get; private set; }
    public CloneStatus CloneStatus { get; private set; }
    public DateTimeOffset? LastSyncedAt { get; private set; }
    public long? RepoSizeBytes { get; private set; }
    public string? CurrentCommitHash { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    private Repository() { }

    public static Repository Create(Guid projectId, string remoteUrl, GitProvider provider, string defaultBranch, DateTimeOffset now)
    {
        return new Repository
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            RemoteUrl = remoteUrl,
            Provider = provider,
            DefaultBranch = defaultBranch,
            CloneStatus = CloneStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateCloneStatus(CloneStatus cloneStatus, DateTimeOffset now)
    {
        CloneStatus = cloneStatus;
        UpdatedAt = now;
    }
    
    public void SoftDelete(DateTimeOffset now)
    {
        if (DeletedAt is not null) return;
        DeletedAt = now;
        CloneStatus = CloneStatus.Disabled;
        UpdatedAt = now;
    }
}
