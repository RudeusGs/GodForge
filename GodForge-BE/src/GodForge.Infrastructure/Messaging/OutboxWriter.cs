using System.Text.Json;
using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Models.Messages;
using GodForge.Domain.Entities.Ops;
using GodForge.Infrastructure.Persistence;

namespace GodForge.Infrastructure.Messaging;

public sealed class OutboxWriter : IOutboxWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly GodForgeDbContext _context;
    private readonly IClock _clock;

    public OutboxWriter(GodForgeDbContext context, IClock clock)
    {
        _context = context;
        _clock = clock;
    }

    public Task EnqueueAsync(
        string queueName,
        WorkerMessage message,
        CancellationToken cancellationToken = default)
        => EnqueueCoreAsync(queueName, message, _clock.UtcNow, cancellationToken);

    public Task EnqueueScheduledAsync(
        string queueName,
        WorkerMessage message,
        DateTimeOffset availableAt,
        CancellationToken cancellationToken = default)
        => EnqueueCoreAsync(queueName, message, availableAt, cancellationToken);

    private async Task EnqueueCoreAsync(
        string queueName,
        WorkerMessage message,
        DateTimeOffset availableAt,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);
        ArgumentNullException.ThrowIfNull(message);

        var now = _clock.UtcNow;
        if (availableAt < now)
            availableAt = now;

        var payload = JsonSerializer.Serialize(message, message.GetType(), JsonOptions);
        var headers = JsonSerializer.Serialize(new
        {
            messageType = message.GetType().Name,
            schemaVersion = message.SchemaVersion
        }, JsonOptions);

        var outboxMessage = OutboxMessage.CreateScheduled(
            aggregateType: "Job",
            aggregateId: message.JobId,
            eventType: queueName,
            payloadJson: payload,
            headersJson: headers,
            correlationId: message.CorrelationId,
            availableAt,
            now);

        await _context.OutboxMessages.AddAsync(outboxMessage, cancellationToken);
    }
}
