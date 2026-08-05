using GodForge.Domain.Common;
using GodForge.Domain.Enums;

namespace GodForge.Domain.Entities.Identity;

public sealed class UserInvite : BaseAuditableEntity
{
    public Guid OrganizationId { get; private set; }
    public string Email { get; private set; } = default!;
    public string NormalizedEmail { get; private set; } = default!;
    public OrganizationRole Role { get; private set; }
    public string TokenHash { get; private set; } = default!;
    public Guid InvitedBy { get; private set; }
    public InviteStatus Status { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? AcceptedAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public long Version { get; private set; }
    public string ConcurrencyStamp { get; private set; } = default!;

    private UserInvite() { }

    public static UserInvite Create(
        Guid organizationId,
        string email,
        OrganizationRole role,
        string tokenHash,
        Guid invitedBy,
        DateTimeOffset expiresAt,
        DateTimeOffset now)
        => new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Email = email.Trim(),
            NormalizedEmail = User.NormalizeEmail(email),
            Role = role,
            TokenHash = tokenHash,
            InvitedBy = invitedBy,
            Status = InviteStatus.Pending,
            ExpiresAt = expiresAt,
            Version = 1,
            ConcurrencyStamp = Guid.NewGuid().ToString("N"),
            CreatedAt = now,
            UpdatedAt = now
        };

    public bool IsActive(DateTimeOffset now) => Status == InviteStatus.Pending && now < ExpiresAt;

    public void Replace(OrganizationRole role, string tokenHash, Guid invitedBy, DateTimeOffset expiresAt, DateTimeOffset now)
    {
        if (Status != InviteStatus.Pending)
            throw new InvalidOperationException("Only a pending invitation can be replaced.");
        Role = role;
        TokenHash = tokenHash;
        InvitedBy = invitedBy;
        ExpiresAt = expiresAt;
        RevokedAt = null;
        Touch(now);
    }

    public void Accept(DateTimeOffset now)
    {
        if (!IsActive(now))
            throw new InvalidOperationException("Invitation is no longer active.");
        Status = InviteStatus.Accepted;
        AcceptedAt = now;
        Touch(now);
    }

    public void Revoke(DateTimeOffset now)
    {
        if (Status == InviteStatus.Pending)
        {
            Status = InviteStatus.Revoked;
            RevokedAt = now;
            Touch(now);
        }
    }

    public void Expire(DateTimeOffset now)
    {
        if (Status == InviteStatus.Pending)
        {
            Status = InviteStatus.Expired;
            Touch(now);
        }
    }

    private void Touch(DateTimeOffset now)
    {
        Version++;
        UpdatedAt = now;
        ConcurrencyStamp = Guid.NewGuid().ToString("N");
    }
}
