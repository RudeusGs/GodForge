using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Ops;

public sealed class OutboxMessage : BaseEntity
{
    public string AggregateType { get; private set; } = default!;
    public Guid? AggregateId { get; private set; }
    public string EventType { get; private set; } = default!;
    public string PayloadJson { get; private set; } = default!;
    public string? HeadersJson { get; private set; }
    public string CorrelationId { get; private set; } = default!;
    public string Status { get; private set; } = default!;
    public int Attempts { get; private set; }
    public DateTimeOffset AvailableAt { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private OutboxMessage() { } // EF Core

    public static OutboxMessage Create(string aggregateType, Guid? aggregateId, string eventType, string payloadJson, string? headersJson, string correlationId, DateTimeOffset now)
    {
        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            AggregateType = aggregateType,
            AggregateId = aggregateId,
            EventType = eventType,
            PayloadJson = payloadJson,
            HeadersJson = headersJson,
            CorrelationId = correlationId,
            Status = "pending",
            Attempts = 0,
            AvailableAt = now,
            CreatedAt = now
        };
    }

    public void RecordAttempt(string? errorMessage, DateTimeOffset nextAvailableAt)
    {
        Attempts++;
        Status = "failed";
        ErrorMessage = errorMessage;
        AvailableAt = nextAvailableAt;
    }

    public void MarkAsProcessed(DateTimeOffset now)
    {
        Status = "processed";
        ProcessedAt = now;
    }
}
