using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Metadata;

public sealed class ParserDiagnostic : BaseEntity
{
    public Guid MetadataRunId { get; private set; }
    public Guid SnapshotId { get; private set; }
    public string FilePath { get; private set; } = default!;
    public string Severity { get; private set; } = default!;
    public string Code { get; private set; } = default!;
    public string Message { get; private set; } = default!;
    public int? Line { get; private set; }
    public int? Column { get; private set; }
    public string? DetailsJson { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private ParserDiagnostic() { } // EF Core

    public static ParserDiagnostic Create(
        Guid metadataRunId, Guid snapshotId, string filePath,
        string severity, string code, string message,
        int? line, int? column, string? detailsJson, DateTimeOffset now)
    {
        return new ParserDiagnostic
        {
            Id = Guid.NewGuid(),
            MetadataRunId = metadataRunId,
            SnapshotId = snapshotId,
            FilePath = filePath,
            Severity = severity,
            Code = code,
            Message = message,
            Line = line,
            Column = column,
            DetailsJson = detailsJson,
            CreatedAt = now
        };
    }
}
