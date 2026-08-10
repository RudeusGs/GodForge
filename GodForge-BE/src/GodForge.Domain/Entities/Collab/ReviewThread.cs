using GodForge.Domain.Common;
using GodForge.Domain.Enums;

namespace GodForge.Domain.Entities.Collab;

public sealed class ReviewThread : BaseAuditableEntity
{
    public Guid ProjectId { get; private set; }
    public Guid RepositoryId { get; private set; }
    public string TargetId { get; private set; } = default!;
    public ReviewThreadStatus Status { get; private set; }
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
            Status = ReviewThreadStatus.Open,
            CreatedBy = createdBy,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Resolve(DateTimeOffset now)
    {
        if (Status == ReviewThreadStatus.Open)
        {
            Status = ReviewThreadStatus.Resolved;
            UpdatedAt = now;
        }
    }

    public void Close(DateTimeOffset now)
    {
        if (Status != ReviewThreadStatus.Closed)
        {
            Status = ReviewThreadStatus.Closed;
            UpdatedAt = now;
        }
    }
}
