using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Collab;

public sealed class Activity : BaseEntity
{
    public Guid ProjectId { get; private set; }
    public Guid? RepositoryId { get; private set; }
    public Guid? ActorId { get; private set; }
    public string Action { get; private set; } = default!;
    public string? TargetType { get; private set; }
    public string? TargetId { get; private set; }
    public string Status { get; private set; } = default!;
    public string? MetadataJson { get; private set; }
    public string CorrelationId { get; private set; } = default!;
    public DateTimeOffset CreatedAt { get; private set; }

    private Activity() { } // EF Core

    public static Activity Create(
        Guid projectId, Guid? repositoryId, Guid? actorId, string action,
        string? targetType, string? targetId, string status,
        string? metadataJson, string correlationId, DateTimeOffset now)
    {
        return new Activity
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            RepositoryId = repositoryId,
            ActorId = actorId,
            Action = action,
            TargetType = targetType,
            TargetId = targetId,
            Status = status,
            MetadataJson = metadataJson,
            CorrelationId = correlationId,
            CreatedAt = now
        };
    }
}
