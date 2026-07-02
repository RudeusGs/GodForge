using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Metadata;

public sealed class MetadataRun : BaseEntity
{
    public Guid ProjectId { get; private set; }
    public Guid RepositoryId { get; private set; }
    public Guid SnapshotId { get; private set; }
    public Guid? JobId { get; private set; }
    public string RunType { get; private set; } = default!;
    public string Status { get; private set; } = default!;
    public string SchemaVersion { get; private set; } = "1.0";
    public int FileCount { get; private set; }
    public int SceneCount { get; private set; }
    public int AssetCount { get; private set; }
    public int ScriptCount { get; private set; }
    public int ResourceCount { get; private set; }
    public int DependencyCount { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private MetadataRun() { } // EF Core

    public static MetadataRun Create(Guid projectId, Guid repositoryId, Guid snapshotId, Guid? jobId, string runType, DateTimeOffset now)
    {
        return new MetadataRun
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            RepositoryId = repositoryId,
            SnapshotId = snapshotId,
            JobId = jobId,
            RunType = runType,
            Status = "running",
            FileCount = 0,
            SceneCount = 0,
            AssetCount = 0,
            ScriptCount = 0,
            ResourceCount = 0,
            DependencyCount = 0,
            StartedAt = now,
            CreatedAt = now
        };
    }

    public void MarkAsCompleted(int fileCount, int sceneCount, int assetCount, int scriptCount, int resourceCount, int dependencyCount, DateTimeOffset now)
    {
        Status = "completed";
        FileCount = fileCount;
        SceneCount = sceneCount;
        AssetCount = assetCount;
        ScriptCount = scriptCount;
        ResourceCount = resourceCount;
        DependencyCount = dependencyCount;
        CompletedAt = now;
    }

    public void MarkAsFailed(DateTimeOffset now)
    {
        Status = "failed";
        CompletedAt = now;
    }
}
