using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Identity;

public sealed class PasswordResetToken : BaseEntity
{
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = default!;
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? UsedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private PasswordResetToken() { } // EF Core

    public static PasswordResetToken Create(Guid userId, string tokenHash, DateTimeOffset expiresAt, DateTimeOffset now)
    {
        return new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
            CreatedAt = now
        };
    }

    public void MarkAsUsed(DateTimeOffset now)
    {
        if (UsedAt is null)
        {
            UsedAt = now;
        }
    }
}
