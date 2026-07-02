using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Ops;

public sealed class InboxMessage : BaseEntity
{
    public string MessageId { get; private set; } = default!;
    public string ConsumerName { get; private set; } = default!;
    public string Status { get; private set; } = default!;
    public DateTimeOffset ReceivedAt { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }
    public string? ErrorMessage { get; private set; }

    private InboxMessage() { } // EF Core

    public static InboxMessage Create(string messageId, string consumerName, DateTimeOffset now)
    {
        return new InboxMessage
        {
            Id = Guid.NewGuid(),
            MessageId = messageId,
            ConsumerName = consumerName,
            Status = "received",
            ReceivedAt = now
        };
    }

    public void MarkAsProcessed(DateTimeOffset now)
    {
        Status = "processed";
        ProcessedAt = now;
    }

    public void MarkAsFailed(string errorMessage, DateTimeOffset now)
    {
        Status = "failed";
        ErrorMessage = errorMessage;
        ProcessedAt = now;
    }
}
