using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Repo;

public sealed class RepositorySnapshot : BaseEntity
{
    public Guid RepositoryId { get; private set; }
    public string CommitHash { get; private set; } = default!;
    public string BranchName { get; private set; } = default!;
    public string Status { get; private set; } = default!;
    public string? MetadataJson { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private RepositorySnapshot() { } // EF Core

    public static RepositorySnapshot Create(Guid repositoryId, string commitHash, string branchName, DateTimeOffset now)
    {
        return new RepositorySnapshot
        {
            Id = Guid.NewGuid(),
            RepositoryId = repositoryId,
            CommitHash = commitHash,
            BranchName = branchName,
            Status = "processing",
            CreatedAt = now
        };
    }

    public void MarkAsReady(string? metadataJson)
    {
        Status = "ready";
        MetadataJson = metadataJson;
    }

    public void MarkAsFailed(string errorMetadataJson)
    {
        Status = "failed";
        MetadataJson = errorMetadataJson;
    }
}
