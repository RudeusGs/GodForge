using GodForge.Domain.Common;
using GodForge.Domain.Enums;

namespace GodForge.Domain.Entities.Storage;

public sealed class ReportExport : BaseEntity
{
    public Guid ProjectId { get; private set; }
    public string Type { get; private set; } = default!;
    public ProcessingStatus Status { get; private set; }
    public string? FilePath { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private ReportExport() { } // EF Core

    public static ReportExport Create(Guid projectId, string type, DateTimeOffset now)
    {
        return new ReportExport
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Type = type,
            Status = ProcessingStatus.Processing,
            CreatedAt = now
        };
    }

    public void MarkAsReady(string filePath, DateTimeOffset expiresAt)
    {
        FilePath = filePath;
        ExpiresAt = expiresAt;
        Status = ProcessingStatus.Ready;
    }

    public void MarkAsFailed()
    {
        Status = ProcessingStatus.Failed;
    }
}
