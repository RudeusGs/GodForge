using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Storage;

public sealed class SceneDiff : BaseEntity
{
    public Guid RepositoryId { get; private set; }
    public string BaseCommit { get; private set; } = default!;
    public string HeadCommit { get; private set; } = default!;
    public string ScenePath { get; private set; } = default!;
    public string? DiffJsonPath { get; private set; }
    public string Status { get; private set; } = default!;
    public DateTimeOffset CreatedAt { get; private set; }

    private SceneDiff() { } // EF Core

    public static SceneDiff Create(Guid repositoryId, string baseCommit, string headCommit, string scenePath, DateTimeOffset now)
    {
        return new SceneDiff
        {
            Id = Guid.NewGuid(),
            RepositoryId = repositoryId,
            BaseCommit = baseCommit,
            HeadCommit = headCommit,
            ScenePath = scenePath,
            Status = "processing",
            CreatedAt = now
        };
    }

    public void MarkAsReady(string diffJsonPath)
    {
        DiffJsonPath = diffJsonPath;
        Status = "ready";
    }

    public void MarkAsFailed()
    {
        Status = "failed";
    }
}
