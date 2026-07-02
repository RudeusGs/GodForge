using GodForge.Domain.Common;
using GodForge.Domain.Enums;

namespace GodForge.Domain.Entities.Core;

public sealed class ProjectInvite : BaseAuditableEntity
{
    public Guid ProjectId { get; private set; }
    public string Email { get; private set; } = default!;
    public string Role { get; private set; } = default!;
    public string TokenHash { get; private set; } = default!;
    public InviteStatus Status { get; private set; }
    public Guid InvitedBy { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? AcceptedAt { get; private set; }

    private ProjectInvite() { } // EF Core

    public static ProjectInvite Create(Guid projectId, string email, string role, string tokenHash, Guid invitedBy, DateTimeOffset expiresAt, DateTimeOffset now)
    {
        return new ProjectInvite
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Email = email,
            Role = role,
            TokenHash = tokenHash,
            Status = InviteStatus.Pending,
            InvitedBy = invitedBy,
            ExpiresAt = expiresAt,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Accept(DateTimeOffset now)
    {
        if (Status != InviteStatus.Pending) return;
        Status = InviteStatus.Accepted;
        AcceptedAt = now;
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
