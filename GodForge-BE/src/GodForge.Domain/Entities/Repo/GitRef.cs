using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Repo;

public sealed class GitRef : BaseAuditableEntity
{
    public Guid RepositoryId { get; private set; }
    public string Type { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string CommitHash { get; private set; } = default!;
    public bool IsDefault { get; private set; }

    private GitRef() { } // EF Core

    public static GitRef Create(Guid repositoryId, string type, string name, string commitHash, bool isDefault, DateTimeOffset now)
    {
        return new GitRef
        {
            Id = Guid.NewGuid(),
            RepositoryId = repositoryId,
            Type = type,
            Name = name,
            CommitHash = commitHash,
            IsDefault = isDefault,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateCommit(string commitHash, DateTimeOffset now)
    {
        CommitHash = commitHash;
        UpdatedAt = now;
    }

    public void SetDefault(bool isDefault, DateTimeOffset now)
    {
        IsDefault = isDefault;
        UpdatedAt = now;
    }
}
