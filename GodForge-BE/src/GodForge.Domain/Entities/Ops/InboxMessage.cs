using GodForge.Domain.Common;
using GodForge.Domain.Enums;

namespace GodForge.Domain.Entities.Ops;

public sealed class InboxMessage : BaseEntity
{
    public string MessageId { get; private set; } = default!;
    public string ConsumerName { get; private set; } = default!;
    public InboxMessageStatus Status { get; private set; }
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
            Status = InboxMessageStatus.Received,
            ReceivedAt = now
        };
    }

    public void MarkAsProcessed(DateTimeOffset now)
    {
        Status = InboxMessageStatus.Processed;
        ProcessedAt = now;
    }

    public void MarkAsFailed(string errorMessage, DateTimeOffset now)
    {
        Status = InboxMessageStatus.Failed;
        ErrorMessage = errorMessage;
        ProcessedAt = now;
    }
}
