using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Identity;

public sealed class EmailChangeRequest : BaseEntity
{
    public Guid UserId { get; private set; }
    public string NewEmail { get; private set; } = default!;
    public string TokenHash { get; private set; } = default!;
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private EmailChangeRequest() { } // EF Core

    public static EmailChangeRequest Create(Guid userId, string newEmail, string tokenHash, DateTimeOffset expiresAt, DateTimeOffset now)
    {
        return new EmailChangeRequest
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            NewEmail = newEmail,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
            CreatedAt = now
        };
    }

    public void MarkAsCompleted(DateTimeOffset now)
    {
        if (CompletedAt is null)
        {
            CompletedAt = now;
        }
    }
}
