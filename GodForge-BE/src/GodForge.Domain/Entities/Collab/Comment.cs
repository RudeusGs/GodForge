using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Collab;

public sealed class Comment : BaseAuditableEntity
{
    public Guid ProjectId { get; private set; }
    public Guid AuthorId { get; private set; }
    public string TargetType { get; private set; } = default!;
    public string TargetId { get; private set; } = default!;
    public string Content { get; private set; } = default!;
    public Guid? ParentId { get; private set; }
    public string Status { get; private set; } = default!;
    public DateTimeOffset? DeletedAt { get; private set; }

    private Comment() { } // EF Core

    public static Comment Create(Guid projectId, Guid authorId, string targetType, string targetId, string content, Guid? parentId, DateTimeOffset now)
    {
        return new Comment
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            AuthorId = authorId,
            TargetType = targetType,
            TargetId = targetId,
            Content = content,
            ParentId = parentId,
            Status = "active",
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateContent(string content, DateTimeOffset now)
    {
        if (Status == "active")
        {
            Content = content;
            UpdatedAt = now;
        }
    }

    public void SoftDelete(DateTimeOffset now)
    {
        if (DeletedAt is null)
        {
            DeletedAt = now;
            Status = "deleted";
            UpdatedAt = now;
        }
    }
}
