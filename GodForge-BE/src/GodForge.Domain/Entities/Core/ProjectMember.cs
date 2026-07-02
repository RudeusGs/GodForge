using GodForge.Domain.Common;
using GodForge.Domain.Enums;

namespace GodForge.Domain.Entities.Core;

public sealed class ProjectMember : BaseAuditableEntity
{
    public Guid ProjectId { get; private set; }
    public Guid UserId { get; private set; }
    public ProjectRole Role { get; private set; }
    public ProjectMemberSource Source { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTimeOffset JoinedAt { get; private set; }
    public DateTimeOffset? RemovedAt { get; private set; }

    private ProjectMember() { }

    public static ProjectMember Create(Guid projectId, Guid userId, ProjectRole role, ProjectMemberSource source, Guid? createdBy, DateTimeOffset now)
    {
        return new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            UserId = userId,
            Role = role,
            Source = source,
            CreatedBy = createdBy,
            JoinedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateRole(ProjectRole newRole, DateTimeOffset now)
    {
        Role = newRole;
        UpdatedAt = now;
    }
}
