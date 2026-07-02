using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Metadata;

public sealed class Scene : BaseEntity
{
    public Guid RepositoryId { get; private set; }
    public Guid SnapshotId { get; private set; }
    public Guid MetadataRunId { get; private set; }
    public string FilePath { get; private set; } = default!;
    public string SceneName { get; private set; } = default!;
    public int? FormatVersion { get; private set; }
    public string? Uid { get; private set; }
    public int? LoadSteps { get; private set; }
    public string? RootNodeName { get; private set; }
    public string? RootNodeType { get; private set; }
    public int NodeCount { get; private set; }
    public string FileHash { get; private set; } = default!;
    public DateTimeOffset ParsedAt { get; private set; }

    private Scene() { } // EF Core

    public static Scene Create(
        Guid repositoryId, Guid snapshotId, Guid metadataRunId,
        string filePath, string sceneName, int? formatVersion,
        string? uid, int? loadSteps, string? rootNodeName,
        string? rootNodeType, int nodeCount, string fileHash, DateTimeOffset now)
    {
        return new Scene
        {
            Id = Guid.NewGuid(),
            RepositoryId = repositoryId,
            SnapshotId = snapshotId,
            MetadataRunId = metadataRunId,
            FilePath = filePath,
            SceneName = sceneName,
            FormatVersion = formatVersion,
            Uid = uid,
            LoadSteps = loadSteps,
            RootNodeName = rootNodeName,
            RootNodeType = rootNodeType,
            NodeCount = nodeCount,
            FileHash = fileHash,
            ParsedAt = now
        };
    }
}
