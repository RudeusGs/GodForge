using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Identity;

public sealed class UserSession : BaseEntity
{
    public Guid UserId { get; private set; }
    public string? DeviceName { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? LastSeenAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public string? RevokedReason { get; private set; }

    private UserSession() { } // EF Core

    public static UserSession Create(Guid userId, string? deviceName, string? ipAddress, string? userAgent, DateTimeOffset expiresAt, DateTimeOffset now)
    {
        return new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DeviceName = deviceName,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            CreatedAt = now,
            LastSeenAt = now,
            ExpiresAt = expiresAt
        };
    }

    public void RecordActivity(DateTimeOffset now)
    {
        if (RevokedAt is null && now < ExpiresAt)
        {
            LastSeenAt = now;
        }
    }

    public void Revoke(string reason, DateTimeOffset now)
    {
        if (RevokedAt is null)
        {
            RevokedAt = now;
            RevokedReason = reason;
        }
    }
}
