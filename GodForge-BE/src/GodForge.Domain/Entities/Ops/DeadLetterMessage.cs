using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Ops;

public sealed class DeadLetterMessage : BaseEntity
{
    public string QueueName { get; private set; } = default!;
    public string MessageId { get; private set; } = default!;
    public string PayloadJson { get; private set; } = default!;
    public string Reason { get; private set; } = default!;
    public string? ErrorDetailsJson { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private DeadLetterMessage() { } // EF Core

    public static DeadLetterMessage Create(string queueName, string messageId, string payloadJson, string reason, string? errorDetailsJson, DateTimeOffset now)
    {
        return new DeadLetterMessage
        {
            Id = Guid.NewGuid(),
            QueueName = queueName,
            MessageId = messageId,
            PayloadJson = payloadJson,
            Reason = reason,
            ErrorDetailsJson = errorDetailsJson,
            CreatedAt = now
        };
    }
}
