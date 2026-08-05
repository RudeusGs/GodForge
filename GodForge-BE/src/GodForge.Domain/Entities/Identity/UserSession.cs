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
    public string ConcurrencyStamp { get; private set; } = default!;

    private UserSession() { }

    public static UserSession Create(Guid userId, string? deviceName, string? ipAddress, string? userAgent, DateTimeOffset expiresAt, DateTimeOffset now)
        => new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DeviceName = string.IsNullOrWhiteSpace(deviceName) ? null : deviceName.Trim(),
            IpAddress = string.IsNullOrWhiteSpace(ipAddress) ? null : ipAddress.Trim(),
            UserAgent = string.IsNullOrWhiteSpace(userAgent) ? null : userAgent.Trim(),
            CreatedAt = now,
            LastSeenAt = now,
            ExpiresAt = expiresAt,
            ConcurrencyStamp = Guid.NewGuid().ToString("N")
        };

    public bool IsActive(DateTimeOffset now) => RevokedAt is null && now < ExpiresAt;

    public void RecordActivity(DateTimeOffset now)
    {
        if (IsActive(now))
        {
            LastSeenAt = now;
            ConcurrencyStamp = Guid.NewGuid().ToString("N");
        }
    }

    public void Revoke(string reason, DateTimeOffset now)
    {
        if (RevokedAt is null)
        {
            RevokedAt = now;
            RevokedReason = reason;
            ConcurrencyStamp = Guid.NewGuid().ToString("N");
        }
    }
}
