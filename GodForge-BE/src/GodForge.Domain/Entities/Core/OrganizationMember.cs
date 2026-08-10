using GodForge.Domain.Common;
using GodForge.Domain.Enums;

namespace GodForge.Domain.Entities.Core;

public sealed class OrganizationMember : BaseAuditableEntity
{
    public Guid OrganizationId { get; private set; }
    public Guid UserId { get; private set; }
    public OrganizationRole Role { get; private set; }
    public MembershipStatus Status { get; private set; }
    public DateTimeOffset JoinedAt { get; private set; }
    public DateTimeOffset? SuspendedAt { get; private set; }
    public DateTimeOffset? RemovedAt { get; private set; }
    public Guid? ChangedByUserId { get; private set; }
    public long Version { get; private set; }

    private OrganizationMember() { }

    public static OrganizationMember CreateOwner(Guid organizationId, Guid userId, DateTimeOffset now)
        => Create(organizationId, userId, OrganizationRole.OrganizationOwner, userId, now);

    public static OrganizationMember Create(Guid organizationId, Guid userId, OrganizationRole role, Guid actorId, DateTimeOffset now)
    {
        EnumGuard.ThrowIfUndefined(role, nameof(role));

        return new OrganizationMember
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            UserId = userId,
            Role = role,
            Status = MembershipStatus.Active,
            JoinedAt = now,
            ChangedByUserId = actorId,
            Version = 1,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Change(OrganizationRole role, MembershipStatus status, Guid actorId, long expectedVersion, DateTimeOffset now)
    {
        EnsureVersion(expectedVersion);
        EnumGuard.ThrowIfUndefined(role, nameof(role));
        EnumGuard.ThrowIfUndefined(status, nameof(status));
        Role = role;
        Status = status;
        SuspendedAt = status == MembershipStatus.Suspended ? now : null;
        RemovedAt = status == MembershipStatus.Removed ? now : null;
        ChangedByUserId = actorId;
        Version++;
        UpdatedAt = now;
    }

    private void EnsureVersion(long expectedVersion)
    {
        if (Version != expectedVersion)
            throw new InvalidOperationException("Concurrency version mismatch.");
    }
}
