using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Identity;

public sealed class UserSetting : BaseAuditableEntity
{
    public Guid UserId { get; private set; }
    public string Theme { get; private set; } = default!;
    public bool NotificationInApp { get; private set; }
    public bool NotificationEmail { get; private set; }
    public string NotificationDigest { get; private set; } = default!;

    private UserSetting() { } // EF Core

    public static UserSetting Create(Guid userId, DateTimeOffset now)
    {
        return new UserSetting
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Theme = "system",
            NotificationInApp = true,
            NotificationEmail = true,
            NotificationDigest = "off",
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Update(string theme, bool notificationInApp, bool notificationEmail, string notificationDigest, DateTimeOffset now)
    {
        Theme = theme;
        NotificationInApp = notificationInApp;
        NotificationEmail = notificationEmail;
        NotificationDigest = notificationDigest;
        UpdatedAt = now;
    }
}
