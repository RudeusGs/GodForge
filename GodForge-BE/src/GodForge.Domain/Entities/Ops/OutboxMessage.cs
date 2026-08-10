using GodForge.Domain.Common;
using GodForge.Domain.Enums;

namespace GodForge.Domain.Entities.Ops;

public sealed class OutboxMessage : BaseEntity
{
    public string AggregateType { get; private set; } = default!;
    public Guid? AggregateId { get; private set; }
    public string EventType { get; private set; } = default!;
    public string PayloadJson { get; private set; } = default!;
    public string? HeadersJson { get; private set; }
    public string CorrelationId { get; private set; } = default!;
    public OutboxMessageStatus Status { get; private set; }
    public int Attempts { get; private set; }
    public DateTimeOffset AvailableAt { get; private set; }
    public Guid? LeaseId { get; private set; }
    public DateTimeOffset? LeaseExpiresAt { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private OutboxMessage() { } // EF Core

    public static OutboxMessage Create(
        string aggregateType,
        Guid? aggregateId,
        string eventType,
        string payloadJson,
        string? headersJson,
        string correlationId,
        DateTimeOffset now)
        => CreateScheduled(
            aggregateType,
            aggregateId,
            eventType,
            payloadJson,
            headersJson,
            correlationId,
            now,
            now);

    public static OutboxMessage CreateScheduled(
        string aggregateType,
        Guid? aggregateId,
        string eventType,
        string payloadJson,
        string? headersJson,
        string correlationId,
        DateTimeOffset availableAt,
        DateTimeOffset now)
        => CreateWithId(
            Guid.NewGuid(),
            aggregateType,
            aggregateId,
            eventType,
            payloadJson,
            headersJson,
            correlationId,
            availableAt,
            now);

    public static OutboxMessage CreateWithId(
        Guid id,
        string aggregateType,
        Guid? aggregateId,
        string eventType,
        string payloadJson,
        string? headersJson,
        string correlationId,
        DateTimeOffset now)
        => CreateWithId(
            id,
            aggregateType,
            aggregateId,
            eventType,
            payloadJson,
            headersJson,
            correlationId,
            now,
            now);

    public static OutboxMessage CreateWithId(
        Guid id,
        string aggregateType,
        Guid? aggregateId,
        string eventType,
        string payloadJson,
        string? headersJson,
        string correlationId,
        DateTimeOffset availableAt,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateType);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(payloadJson);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        if (availableAt < now)
            throw new ArgumentOutOfRangeException(nameof(availableAt), "Outbox availability cannot be before creation time.");

        return new OutboxMessage
        {
            Id = id,
            AggregateType = aggregateType,
            AggregateId = aggregateId,
            EventType = eventType,
            PayloadJson = payloadJson,
            HeadersJson = headersJson,
            CorrelationId = correlationId,
            Status = OutboxMessageStatus.Pending,
            Attempts = 0,
            AvailableAt = availableAt,
            CreatedAt = now
        };
    }

    public void MarkProcessing(Guid leaseId, DateTimeOffset leaseExpiresAt, DateTimeOffset now)
    {
        if (leaseId == Guid.Empty)
            throw new ArgumentException("A non-empty lease identifier is required.", nameof(leaseId));
        if (leaseExpiresAt <= now)
            throw new ArgumentOutOfRangeException(nameof(leaseExpiresAt), "The outbox lease must expire in the future.");
        if (Status is OutboxMessageStatus.Processed or OutboxMessageStatus.DeadLettered)
            throw new InvalidOperationException("A terminal outbox message cannot be claimed again.");

        Status = OutboxMessageStatus.Processing;
        AvailableAt = leaseExpiresAt;
        LeaseId = leaseId;
        LeaseExpiresAt = leaseExpiresAt;
        ErrorMessage = null;
    }

    public bool IsOwnedBy(Guid leaseId)
        => Status == OutboxMessageStatus.Processing && LeaseId == leaseId;

    public void RenewLease(Guid leaseId, DateTimeOffset leaseExpiresAt, DateTimeOffset now)
    {
        EnsureLease(leaseId);
        if (leaseExpiresAt <= now)
            throw new ArgumentOutOfRangeException(nameof(leaseExpiresAt), "The renewed outbox lease must expire in the future.");

        AvailableAt = leaseExpiresAt;
        LeaseExpiresAt = leaseExpiresAt;
    }

    public void RecordAttempt(Guid leaseId, string? errorMessage, DateTimeOffset nextAvailableAt, DateTimeOffset now)
    {
        EnsureLease(leaseId);
        if (nextAvailableAt <= now)
            throw new ArgumentOutOfRangeException(nameof(nextAvailableAt), "The next outbox attempt must be scheduled in the future.");

        Attempts++;
        Status = OutboxMessageStatus.Failed;
        ErrorMessage = errorMessage;
        AvailableAt = nextAvailableAt;
        LeaseId = null;
        LeaseExpiresAt = null;
    }

    public void MarkDeadLettered(Guid leaseId, string? errorMessage, DateTimeOffset now)
    {
        EnsureLease(leaseId);

        Attempts++;
        Status = OutboxMessageStatus.DeadLettered;
        ErrorMessage = errorMessage;
        AvailableAt = now;
        LeaseId = null;
        LeaseExpiresAt = null;
    }

    public void MarkAsProcessed(Guid leaseId, DateTimeOffset now)
    {
        EnsureLease(leaseId);
        Status = OutboxMessageStatus.Processed;
        ProcessedAt = now;
        ErrorMessage = null;
        LeaseId = null;
        LeaseExpiresAt = null;
    }

    private void EnsureLease(Guid leaseId)
    {
        if (Status != OutboxMessageStatus.Processing || LeaseId != leaseId)
            throw new InvalidOperationException("The outbox message is not owned by the supplied lease.");
    }
}
