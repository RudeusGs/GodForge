using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Metadata;

public sealed class Script : BaseEntity
{
    public Guid RepositoryId { get; private set; }
    public Guid SnapshotId { get; private set; }
    public Guid MetadataRunId { get; private set; }
    public string FilePath { get; private set; } = default!;
    public string? ClassName { get; private set; }
    public string? ExtendsType { get; private set; }
    public int LineCount { get; private set; }
    public int PublicMethodCount { get; private set; }
    public int SignalCount { get; private set; }
    public int ExportedPropertyCount { get; private set; }
    public string FileHash { get; private set; } = default!;
    public string? ParseSummaryJson { get; private set; }

    private Script() { } // EF Core

    public static Script Create(
        Guid repositoryId, Guid snapshotId, Guid metadataRunId,
        string filePath, string? className, string? extendsType,
        int lineCount, int publicMethodCount, int signalCount,
        int exportedPropertyCount, string fileHash, string? parseSummaryJson)
    {
        return new Script
        {
            Id = Guid.NewGuid(),
            RepositoryId = repositoryId,
            SnapshotId = snapshotId,
            MetadataRunId = metadataRunId,
            FilePath = filePath,
            ClassName = className,
            ExtendsType = extendsType,
            LineCount = lineCount,
            PublicMethodCount = publicMethodCount,
            SignalCount = signalCount,
            ExportedPropertyCount = exportedPropertyCount,
            FileHash = fileHash,
            ParseSummaryJson = parseSummaryJson
        };
    }
}
