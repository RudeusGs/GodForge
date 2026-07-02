using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Identity;

public sealed class RefreshToken : BaseEntity
{
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = default!;
    public string? ReplacedByTokenHash { get; private set; }
    public string? DeviceName { get; private set; }
    public string? IpAddress { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }

    private RefreshToken() { } // EF Core

    public static RefreshToken Create(Guid userId, string tokenHash, string? deviceName, string? ipAddress, DateTimeOffset expiresAt, DateTimeOffset now)
    {
        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            DeviceName = deviceName,
            IpAddress = ipAddress,
            ExpiresAt = expiresAt,
            CreatedAt = now
        };
    }

    public void Revoke(DateTimeOffset now, string? replacedByTokenHash = null)
    {
        RevokedAt = now;
        ReplacedByTokenHash = replacedByTokenHash;
    }

    public bool CheckIsActive(DateTimeOffset now) => RevokedAt == null && now < ExpiresAt;
}
