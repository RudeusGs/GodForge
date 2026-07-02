using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Metadata;

public sealed class Dependency : BaseEntity
{
    public Guid RepositoryId { get; private set; }
    public Guid SnapshotId { get; private set; }
    public Guid MetadataRunId { get; private set; }
    public string SourceType { get; private set; } = default!;
    public string SourcePath { get; private set; } = default!;
    public string TargetType { get; private set; } = default!;
    public string TargetPath { get; private set; } = default!;
    public string Relation { get; private set; } = default!;
    public bool IsMissing { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private Dependency() { } // EF Core

    public static Dependency Create(
        Guid repositoryId, Guid snapshotId, Guid metadataRunId,
        string sourceType, string sourcePath,
        string targetType, string targetPath,
        string relation, bool isMissing, DateTimeOffset now)
    {
        return new Dependency
        {
            Id = Guid.NewGuid(),
            RepositoryId = repositoryId,
            SnapshotId = snapshotId,
            MetadataRunId = metadataRunId,
            SourceType = sourceType,
            SourcePath = sourcePath,
            TargetType = targetType,
            TargetPath = targetPath,
            Relation = relation,
            IsMissing = isMissing,
            CreatedAt = now
        };
    }
}
