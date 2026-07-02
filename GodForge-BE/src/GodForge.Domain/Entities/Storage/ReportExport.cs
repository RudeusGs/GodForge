using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Storage;

public sealed class ReportExport : BaseEntity
{
    public Guid ProjectId { get; private set; }
    public string Type { get; private set; } = default!;
    public string Status { get; private set; } = default!;
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
            Status = "processing",
            CreatedAt = now
        };
    }

    public void MarkAsReady(string filePath, DateTimeOffset expiresAt)
    {
        FilePath = filePath;
        ExpiresAt = expiresAt;
        Status = "ready";
    }

    public void MarkAsFailed()
    {
        Status = "failed";
    }
}
