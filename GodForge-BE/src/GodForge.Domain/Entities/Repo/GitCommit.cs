using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Repo;

public sealed class GitCommit : BaseEntity
{
    public Guid RepositoryId { get; private set; }
    public string Hash { get; private set; } = default!;
    public string TreeHash { get; private set; } = default!;
    public string AuthorName { get; private set; } = default!;
    public string AuthorEmail { get; private set; } = default!;
    public string Message { get; private set; } = default!;
    public DateTimeOffset AuthoredAt { get; private set; }
    public DateTimeOffset CommittedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private GitCommit() { } // EF Core

    public static GitCommit Create(Guid repositoryId, string hash, string treeHash, string authorName, string authorEmail, string message, DateTimeOffset authoredAt, DateTimeOffset committedAt, DateTimeOffset now)
    {
        return new GitCommit
        {
            Id = Guid.NewGuid(),
            RepositoryId = repositoryId,
            Hash = hash,
            TreeHash = treeHash,
            AuthorName = authorName,
            AuthorEmail = authorEmail,
            Message = message,
            AuthoredAt = authoredAt,
            CommittedAt = committedAt,
            CreatedAt = now
        };
    }
}
