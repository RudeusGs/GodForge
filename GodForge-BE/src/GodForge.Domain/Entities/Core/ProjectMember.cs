using GodForge.Domain.Common;
using GodForge.Domain.Enums;

namespace GodForge.Domain.Entities.Core;

public sealed class ProjectMember : BaseAuditableEntity
{
    public Guid ProjectId { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid UserId { get; private set; }
    public ProjectRole Role { get; private set; }
    public MembershipStatus Status { get; private set; }
    public ProjectMemberSource Source { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTimeOffset JoinedAt { get; private set; }
    public DateTimeOffset? RemovedAt { get; private set; }
    public DateTimeOffset? SuspendedAt { get; private set; }
    public long Version { get; private set; }

    private ProjectMember() { }

    public static ProjectMember Create(Guid projectId, Guid organizationId, Guid userId, ProjectRole role, ProjectMemberSource source, Guid? createdBy, DateTimeOffset now)
    {
        EnumGuard.ThrowIfUndefined(role, nameof(role));
        EnumGuard.ThrowIfUndefined(source, nameof(source));

        return new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            OrganizationId = organizationId,
            UserId = userId,
            Role = role,
            Status = MembershipStatus.Active,
            Source = source,
            CreatedBy = createdBy,
            JoinedAt = now,
            Version = 1,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateRole(ProjectRole newRole, long expectedVersion, DateTimeOffset now)
    {
        EnsureVersion(expectedVersion);
        if (Status != MembershipStatus.Active)
            throw new InvalidOperationException("Only active memberships can change role.");
        EnumGuard.ThrowIfUndefined(newRole, nameof(newRole));
        Role = newRole;
        Version++;
        UpdatedAt = now;
    }

    public void Suspend(DateTimeOffset now)
    {
        Status = MembershipStatus.Suspended;
        SuspendedAt = now;
        RemovedAt = null;
        Version++;
        UpdatedAt = now;
    }

    public void Remove(DateTimeOffset now)
    {
        Status = MembershipStatus.Removed;
        RemovedAt = now;
        SuspendedAt = null;
        Version++;
        UpdatedAt = now;
    }

    public void Reactivate(ProjectRole role, DateTimeOffset now)
    {
        EnumGuard.ThrowIfUndefined(role, nameof(role));
        Role = role;
        Status = MembershipStatus.Active;
        RemovedAt = null;
        SuspendedAt = null;
        Version++;
        UpdatedAt = now;
    }

    private void EnsureVersion(long expectedVersion)
    {
        if (Version != expectedVersion)
            throw new InvalidOperationException("Concurrency version mismatch.");
    }
}
