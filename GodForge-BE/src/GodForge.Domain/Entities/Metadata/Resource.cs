using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Metadata;

public sealed class Resource : BaseEntity
{
    public Guid RepositoryId { get; private set; }
    public Guid SnapshotId { get; private set; }
    public Guid MetadataRunId { get; private set; }
    public string FilePath { get; private set; } = default!;
    public string ResourceType { get; private set; } = default!;
    public string? Uid { get; private set; }
    public string FileHash { get; private set; } = default!;
    public string? PropertiesJson { get; private set; }

    private Resource() { } // EF Core

    public static Resource Create(
        Guid repositoryId, Guid snapshotId, Guid metadataRunId,
        string filePath, string resourceType, string? uid,
        string fileHash, string? propertiesJson)
    {
        return new Resource
        {
            Id = Guid.NewGuid(),
            RepositoryId = repositoryId,
            SnapshotId = snapshotId,
            MetadataRunId = metadataRunId,
            FilePath = filePath,
            ResourceType = resourceType,
            Uid = uid,
            FileHash = fileHash,
            PropertiesJson = propertiesJson
        };
    }
}
