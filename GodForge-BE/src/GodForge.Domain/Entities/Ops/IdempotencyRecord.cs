using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Ops;

public sealed class IdempotencyRecord : BaseEntity
{
    public Guid ActorUserId { get; private set; }
    public string Operation { get; private set; } = default!;
    public string Key { get; private set; } = default!;
    public string RequestHash { get; private set; } = default!;
    public string ResourceType { get; private set; } = default!;
    public Guid ResourceId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private IdempotencyRecord() { }

    public static IdempotencyRecord Create(
        Guid actorUserId,
        string operation,
        string key,
        string requestHash,
        string resourceType,
        Guid resourceId,
        DateTimeOffset now)
        => new()
        {
            Id = Guid.NewGuid(),
            ActorUserId = actorUserId,
            Operation = operation,
            Key = key,
            RequestHash = requestHash,
            ResourceType = resourceType,
            ResourceId = resourceId,
            CreatedAt = now
        };
}
