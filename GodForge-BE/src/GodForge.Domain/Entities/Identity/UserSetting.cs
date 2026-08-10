using GodForge.Domain.Common;
using GodForge.Domain.Enums;

namespace GodForge.Domain.Entities.Identity;

public sealed class UserSetting : BaseAuditableEntity
{
    public Guid UserId { get; private set; }
    public Theme Theme { get; private set; }
    public bool NotificationInApp { get; private set; }
    public bool NotificationEmail { get; private set; }
    public NotificationDigest NotificationDigest { get; private set; }

    private UserSetting() { } // EF Core

    public static UserSetting Create(Guid userId, DateTimeOffset now)
    {
        return new UserSetting
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Theme = Theme.Light,
            NotificationInApp = true,
            NotificationEmail = true,
            NotificationDigest = NotificationDigest.Off,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Update(Theme theme, bool notificationInApp, bool notificationEmail, NotificationDigest notificationDigest, DateTimeOffset now)
    {
        Theme = theme;
        NotificationInApp = notificationInApp;
        NotificationEmail = notificationEmail;
        NotificationDigest = notificationDigest;
        UpdatedAt = now;
    }
}
