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

    public async Task EnqueueAsync(
        string queueName,
        WorkerMessage message,
        CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Serialize(message, message.GetType(), JsonOptions);
        var headers = JsonSerializer.Serialize(new
        {
            messageType = message.GetType().Name,
            schemaVersion = message.SchemaVersion
        }, JsonOptions);

        var outboxMessage = OutboxMessage.Create(
            aggregateType: "Job",
            aggregateId: message.JobId,
            eventType: queueName,
            payloadJson: payload,
            headersJson: headers,
            correlationId: message.CorrelationId,
            now: _clock.UtcNow);

        await _context.OutboxMessages.AddAsync(outboxMessage, cancellationToken);
    }
}
