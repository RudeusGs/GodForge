using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Repo;

public sealed class RepositoryFile : BaseAuditableEntity
{
    public Guid RepositoryId { get; private set; }
    public string Path { get; private set; } = default!;
    public string Type { get; private set; } = default!;
    public long Size { get; private set; }
    public string LastCommitHash { get; private set; } = default!;

    private RepositoryFile() { } // EF Core

    public static RepositoryFile Create(Guid repositoryId, string path, string type, long size, string lastCommitHash, DateTimeOffset now)
    {
        return new RepositoryFile
        {
            Id = Guid.NewGuid(),
            RepositoryId = repositoryId,
            Path = path,
            Type = type,
            Size = size,
            LastCommitHash = lastCommitHash,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Update(long size, string lastCommitHash, DateTimeOffset now)
    {
        Size = size;
        LastCommitHash = lastCommitHash;
        UpdatedAt = now;
    }
}
