using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Collab;

public sealed class ReviewThread : BaseAuditableEntity
{
    public Guid ProjectId { get; private set; }
    public Guid RepositoryId { get; private set; }
    public string TargetId { get; private set; } = default!;
    public string Status { get; private set; } = default!;
    public Guid CreatedBy { get; private set; }

    private ReviewThread() { } // EF Core

    public static ReviewThread Create(Guid projectId, Guid repositoryId, string targetId, Guid createdBy, DateTimeOffset now)
    {
        return new ReviewThread
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            RepositoryId = repositoryId,
            TargetId = targetId,
            Status = "open",
            CreatedBy = createdBy,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Resolve(DateTimeOffset now)
    {
        if (Status == "open")
        {
            Status = "resolved";
            UpdatedAt = now;
        }
    }

    public void Close(DateTimeOffset now)
    {
        if (Status != "closed")
        {
            Status = "closed";
            UpdatedAt = now;
        }
    }
}
