using GodForge.Domain.Common;
using GodForge.Domain.Enums;

namespace GodForge.Domain.Entities.Repo;

public sealed class GitRepository : BaseAuditableEntity, ISoftDeletable
{
    public Guid ProjectId { get; private set; }
    public RepositoryMode Mode { get; private set; }
    public GitProvider Provider { get; private set; }
    public string RemoteUrl { get; private set; } = default!;
    public string CloneUrlSanitized { get; private set; } = default!;
    public string DefaultBranch { get; private set; } = default!;
    public string? ExternalRepositoryId { get; private set; }
    public string? HostedRepositoryId { get; private set; }
    public string? WorkspaceKey { get; private set; }
    public GitRepositoryStatus GitRepositoryStatus { get; private set; }
    public bool AutoAnalyzeEnabled { get; private set; }
    public DateTimeOffset? LastSyncedAt { get; private set; }
    public long? RepoSizeBytes { get; private set; }
    public string? CurrentCommitHash { get; private set; }
    public string? LastErrorCode { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    private GitRepository() { }

    public static GitRepository Create(
        Guid projectId,
        string remoteUrl,
        GitProvider provider,
        string defaultBranch,
        DateTimeOffset now)
        => CreateLinked(projectId, remoteUrl, provider, defaultBranch, null, false, now);

    public static GitRepository CreateLinked(
        Guid projectId,
        string remoteUrl,
        GitProvider provider,
        string defaultBranch,
        string? externalRepositoryId,
        bool autoAnalyzeEnabled,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(remoteUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultBranch);

        return new GitRepository
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Mode = RepositoryMode.ExternalLinked,
            Provider = provider,
            RemoteUrl = remoteUrl.Trim(),
            CloneUrlSanitized = SanitizeCloneUrl(remoteUrl),
            DefaultBranch = defaultBranch.Trim(),
            ExternalRepositoryId = externalRepositoryId,
            AutoAnalyzeEnabled = autoAnalyzeEnabled,
            GitRepositoryStatus = GitRepositoryStatus.Configured,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public static GitRepository CreateHosted(
        Guid projectId,
        string remoteUrl,
        string hostedRepositoryId,
        string defaultBranch,
        bool autoAnalyzeEnabled,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(remoteUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(hostedRepositoryId);
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultBranch);

        return new GitRepository
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Mode = RepositoryMode.InternalHosted,
            Provider = GitProvider.Forgejo,
            RemoteUrl = remoteUrl.Trim(),
            CloneUrlSanitized = SanitizeCloneUrl(remoteUrl),
            HostedRepositoryId = hostedRepositoryId.Trim(),
            DefaultBranch = defaultBranch.Trim(),
            AutoAnalyzeEnabled = autoAnalyzeEnabled,
            GitRepositoryStatus = GitRepositoryStatus.Configured,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void MarkSyncStarted(string workspaceKey, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceKey);
        WorkspaceKey = workspaceKey;
        GitRepositoryStatus = GitRepositoryStatus.Cloning;
        LastErrorCode = null;
        UpdatedAt = now;
    }

    public void MarkSynchronized(string commitHash, long repositorySizeBytes, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commitHash);
        CurrentCommitHash = commitHash.Trim();
        RepoSizeBytes = repositorySizeBytes;
        LastSyncedAt = now;
        LastErrorCode = null;
        GitRepositoryStatus = GitRepositoryStatus.Ready;
        UpdatedAt = now;
    }

    public void MarkError(string errorCode, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);
        LastErrorCode = errorCode.Trim();
        GitRepositoryStatus = GitRepositoryStatus.Error;
        UpdatedAt = now;
    }

    public void UpdateSettings(string defaultBranch, bool autoAnalyzeEnabled, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultBranch);
        DefaultBranch = defaultBranch.Trim();
        AutoAnalyzeEnabled = autoAnalyzeEnabled;
        UpdatedAt = now;
    }

    public void UpdateGitRepositoryStatus(GitRepositoryStatus newStatus, DateTimeOffset now)
    {
        GitRepositoryStatus = newStatus;
        UpdatedAt = now;
    }

    public void SoftDelete(DateTimeOffset now)
    {
        if (DeletedAt is not null)
        {
            return;
        }

        DeletedAt = now;
        GitRepositoryStatus = GitRepositoryStatus.Disabled;
        UpdatedAt = now;
    }

    private static string SanitizeCloneUrl(string remoteUrl)
    {
        if (!Uri.TryCreate(remoteUrl, UriKind.Absolute, out var uri))
        {
            return remoteUrl.Trim();
        }

        var builder = new UriBuilder(uri)
        {
            UserName = string.Empty,
            Password = string.Empty
        };

        return builder.Uri.ToString();
    }
}
