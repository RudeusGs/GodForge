using System.Text.Json;
using GodForge.Application.Common.Constants;
using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Models.Messages;
using GodForge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GodForge.Infrastructure.Messaging;

public sealed class OutboxDispatcherService : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan EmptyPollDelay = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan ClaimLease = TimeSpan.FromMinutes(1);
    private const int BatchSize = 20;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IClock _clock;
    private readonly ILogger<OutboxDispatcherService> _logger;

    public OutboxDispatcherService(
        IServiceScopeFactory scopeFactory,
        IClock clock,
        ILogger<OutboxDispatcherService> logger)
    {
        _scopeFactory = scopeFactory;
        _clock = clock;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var claimedIds = await ClaimBatchAsync(stoppingToken);
                if (claimedIds.Count == 0)
                {
                    await Task.Delay(EmptyPollDelay, stoppingToken);
                    continue;
                }

                foreach (var id in claimedIds)
                {
                    await DispatchAsync(id, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Outbox dispatcher loop failed");
                await Task.Delay(EmptyPollDelay, stoppingToken);
            }
        }
    }

    private async Task<IReadOnlyList<Guid>> ClaimBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GodForgeDbContext>();
        var now = _clock.UtcNow;

        if (!context.Database.IsRelational())
        {
            var inMemoryMessages = await context.OutboxMessages
                .Where(message =>
                    (message.Status == "pending" ||
                     message.Status == "failed" ||
                     message.Status == "processing") &&
                    message.AvailableAt <= now)
                .OrderBy(message => message.CreatedAt)
                .Take(BatchSize)
                .ToListAsync(cancellationToken);

            foreach (var message in inMemoryMessages)
            {
                message.MarkProcessing(now.Add(ClaimLease));
            }

            await context.SaveChangesAsync(cancellationToken);
            return inMemoryMessages.Select(message => message.Id).ToList();
        }

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var messages = await context.OutboxMessages
            .FromSqlInterpolated($$"""
                SELECT *
                FROM ops.outbox_messages
                WHERE status IN ('pending', 'failed', 'processing')
                  AND available_at <= {{now}}
                ORDER BY created_at
                FOR UPDATE SKIP LOCKED
                LIMIT {{BatchSize}}
                """)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            message.MarkProcessing(now.Add(ClaimLease));
        }

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return messages.Select(message => message.Id).ToList();
    }

    private async Task DispatchAsync(Guid outboxMessageId, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GodForgeDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IJobPublisher>();
        var message = await context.OutboxMessages
            .FirstOrDefaultAsync(item => item.Id == outboxMessageId, cancellationToken);

        if (message is null || message.Status == "processed")
        {
            return;
        }

        try
        {
            var workerMessage = DeserializeMessage(message.EventType, message.PayloadJson);
            await publisher.PublishAsync(message.EventType, workerMessage, cancellationToken);
            message.MarkAsProcessed(_clock.UtcNow);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            var delaySeconds = Math.Min(300, Math.Pow(2, Math.Min(message.Attempts, 6)) * 5);
            message.RecordAttempt(
                SanitizeError(exception.Message),
                _clock.UtcNow.AddSeconds(delaySeconds));
            _logger.LogWarning(
                exception,
                "Outbox message {OutboxMessageId} could not be published on attempt {Attempt}",
                message.Id,
                message.Attempts);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static WorkerMessage DeserializeMessage(string queueName, string payloadJson)
        => queueName switch
        {
            WorkerQueueNames.RepositoryPipeline =>
                JsonSerializer.Deserialize<RepositoryAnalysisJobMessage>(payloadJson, JsonOptions)
                ?? throw new JsonException("Repository analysis outbox payload is empty."),
            _ => throw new NotSupportedException($"Outbox queue '{queueName}' is not supported.")
        };

    private static string SanitizeError(string message)
    {
        const int maxLength = 500;
        return message.Length <= maxLength ? message : message[..maxLength];
    }
}
