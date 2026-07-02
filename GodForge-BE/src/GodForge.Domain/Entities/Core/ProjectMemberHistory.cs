using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Core;

public sealed class ProjectMemberHistory : BaseEntity
{
    public Guid ProjectId { get; private set; }
    public Guid UserId { get; private set; }
    public string Role { get; private set; } = default!;
    public string Action { get; private set; } = default!;
    public Guid ActorId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private ProjectMemberHistory() { } // EF Core

    public static ProjectMemberHistory Create(Guid projectId, Guid userId, string role, string action, Guid actorId, DateTimeOffset now)
    {
        return new ProjectMemberHistory
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            UserId = userId,
            Role = role,
            Action = action,
            ActorId = actorId,
            CreatedAt = now
        };
    }
}
