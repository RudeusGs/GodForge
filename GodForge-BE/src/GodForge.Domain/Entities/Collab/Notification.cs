using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Collab;

public sealed class Notification : BaseEntity
{
    public Guid UserId { get; private set; }
    public Guid? ProjectId { get; private set; }
    public string Type { get; private set; } = default!;
    public string Title { get; private set; } = default!;
    public string Message { get; private set; } = default!;
    public string? Link { get; private set; }
    public bool IsRead { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private Notification() { } // EF Core

    public static Notification Create(Guid userId, Guid? projectId, string type, string title, string message, string? link, DateTimeOffset now)
    {
        return new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ProjectId = projectId,
            Type = type,
            Title = title,
            Message = message,
            Link = link,
            IsRead = false,
            CreatedAt = now
        };
    }

    public void MarkAsRead()
    {
        IsRead = true;
    }
}
