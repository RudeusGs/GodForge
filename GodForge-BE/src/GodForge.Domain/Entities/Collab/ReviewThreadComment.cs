using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Collab;

public sealed class ReviewThreadComment : BaseAuditableEntity
{
    public Guid ThreadId { get; private set; }
    public Guid AuthorId { get; private set; }
    public string Content { get; private set; } = default!;

    private ReviewThreadComment() { } // EF Core

    public static ReviewThreadComment Create(Guid threadId, Guid authorId, string content, DateTimeOffset now)
    {
        return new ReviewThreadComment
        {
            Id = Guid.NewGuid(),
            ThreadId = threadId,
            AuthorId = authorId,
            Content = content,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateContent(string content, DateTimeOffset now)
    {
        Content = content;
        UpdatedAt = now;
    }
}
