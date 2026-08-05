using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Identity;

public sealed class RefreshToken : BaseEntity
{
    public Guid UserId { get; private set; }
    public Guid SessionId { get; private set; }
    public Guid FamilyId { get; private set; }
    public string TokenHash { get; private set; } = default!;
    public string? ReplacedByTokenHash { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public string? RevokedReason { get; private set; }
    public string ConcurrencyStamp { get; private set; } = default!;

    private RefreshToken() { }

    public static RefreshToken Create(
        Guid userId,
        Guid sessionId,
        Guid familyId,
        string tokenHash,
        DateTimeOffset expiresAt,
        DateTimeOffset now)
        => new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SessionId = sessionId,
            FamilyId = familyId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
            CreatedAt = now,
            ConcurrencyStamp = Guid.NewGuid().ToString("N")
        };

    public void Revoke(DateTimeOffset now, string reason, string? replacedByTokenHash = null)
    {
        if (RevokedAt is not null)
            return;

        RevokedAt = now;
        RevokedReason = reason;
        ReplacedByTokenHash = replacedByTokenHash;
        ConcurrencyStamp = Guid.NewGuid().ToString("N");
    }

    public bool CheckIsActive(DateTimeOffset now) => RevokedAt is null && now < ExpiresAt;
}
