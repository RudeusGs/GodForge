using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Governance;

public sealed class ArchiveRecord : BaseEntity
{
    public string SourceTable { get; private set; } = default!;
    public Guid? SourceId { get; private set; }
    public Guid? ArtifactId { get; private set; }
    public DateTimeOffset ArchivedAt { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }

    private ArchiveRecord() { } // EF Core

    public static ArchiveRecord Create(
        string sourceTable, Guid? sourceId, Guid? artifactId,
        DateTimeOffset? expiresAt, DateTimeOffset now)
    {
        return new ArchiveRecord
        {
            Id = Guid.NewGuid(),
            SourceTable = sourceTable,
            SourceId = sourceId,
            ArtifactId = artifactId,
            ExpiresAt = expiresAt,
            ArchivedAt = now
        };
    }
}
