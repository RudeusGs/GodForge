using GodForge.Domain.Common;
using GodForge.Domain.Enums;

namespace GodForge.Domain.Entities.Identity;

public sealed class UserInvite : BaseAuditableEntity
{
    public string Email { get; private set; } = default!;
    public string TokenHash { get; private set; } = default!;
    public Guid InvitedBy { get; private set; }
    public InviteStatus Status { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }

    private UserInvite() { } // EF Core

    public static UserInvite Create(string email, string tokenHash, Guid invitedBy, DateTimeOffset expiresAt, DateTimeOffset now)
    {
        return new UserInvite
        {
            Id = Guid.NewGuid(),
            Email = email,
            TokenHash = tokenHash,
            InvitedBy = invitedBy,
            Status = InviteStatus.Pending,
            ExpiresAt = expiresAt,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Accept(DateTimeOffset now)
    {
        if (Status != InviteStatus.Pending) return;
        Status = InviteStatus.Accepted;
        UpdatedAt = now;
    }

    public void Revoke(DateTimeOffset now)
    {
        if (Status != InviteStatus.Pending) return;
        Status = InviteStatus.Revoked;
        UpdatedAt = now;
    }

    public void Expire(DateTimeOffset now)
    {
        if (Status != InviteStatus.Pending) return;
        Status = InviteStatus.Expired;
        UpdatedAt = now;
    }
}
