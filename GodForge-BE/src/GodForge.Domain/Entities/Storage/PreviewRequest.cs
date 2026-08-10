using GodForge.Domain.Common;
using GodForge.Domain.Enums;

namespace GodForge.Domain.Entities.Storage;

public sealed class PreviewRequest : BaseEntity
{
    public Guid ProjectId { get; private set; }
    public string AssetId { get; private set; } = default!;
    public ProcessingStatus Status { get; private set; }
    public string? OutputPath { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private PreviewRequest() { } // EF Core

    public static PreviewRequest Create(Guid projectId, string assetId, DateTimeOffset now)
    {
        return new PreviewRequest
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            AssetId = assetId,
            Status = ProcessingStatus.Processing,
            CreatedAt = now
        };
    }

    public void MarkAsReady(string outputPath)
    {
        OutputPath = outputPath;
        Status = ProcessingStatus.Ready;
    }

    public void MarkAsFailed(string errorMessage)
    {
        ErrorMessage = errorMessage;
        Status = ProcessingStatus.Failed;
    }
}
