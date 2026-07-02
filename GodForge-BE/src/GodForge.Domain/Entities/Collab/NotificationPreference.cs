using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Collab;

public sealed class NotificationPreference : BaseAuditableEntity
{
    public Guid UserId { get; private set; }
    public string EventType { get; private set; } = default!;
    public bool InApp { get; private set; }
    public bool Email { get; private set; }

    private NotificationPreference() { } // EF Core

    public static NotificationPreference Create(Guid userId, string eventType, bool inApp, bool email, DateTimeOffset now)
    {
        return new NotificationPreference
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EventType = eventType,
            InApp = inApp,
            Email = email,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Update(bool inApp, bool email, DateTimeOffset now)
    {
        InApp = inApp;
        Email = email;
        UpdatedAt = now;
    }
}
