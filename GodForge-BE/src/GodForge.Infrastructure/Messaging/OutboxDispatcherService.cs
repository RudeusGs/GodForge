using System.Collections.Concurrent;
using System.Text.Json;
using GodForge.Application.Common.Constants;
using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Models.Messages;
using GodForge.Domain.Entities.Ops;
using GodForge.Domain.Enums;
using GodForge.Infrastructure.Configuration;
using GodForge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GodForge.Infrastructure.Messaging;

public sealed class OutboxDispatcherService : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan EmptyPollDelay = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan ClaimLease = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan LeaseRenewalInterval = TimeSpan.FromSeconds(20);
    private const int BatchSize = 20;
    private const string RetiredProviderReconciliationEventType = "provider.reconciliation.requested";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IClock _clock;
    private readonly OutboxDispatcherSettings _settings;
    private readonly ILogger<OutboxDispatcherService> _logger;

    public OutboxDispatcherService(
        IServiceScopeFactory scopeFactory,
        IClock clock,
        IOptions<OutboxDispatcherSettings> settings,
        ILogger<OutboxDispatcherService> logger)
    {
        _scopeFactory = scopeFactory;
        _clock = clock;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var claims = await ClaimBatchAsync(stoppingToken);
                if (claims.Count == 0)
                {
                    await Task.Delay(EmptyPollDelay, stoppingToken);
                    continue;
                }

                var activeClaims = new ConcurrentDictionary<Guid, OutboxClaim>(
                    claims.ToDictionary(claim => claim.MessageId));
                using var renewalCancellation = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                var renewalTask = RenewClaimBatchAsync(activeClaims, renewalCancellation.Token);
                try
                {
                    foreach (var claim in claims)
                    {
                        try
                        {
                            await DispatchAsync(claim, stoppingToken);
                        }
                        finally
                        {
                            activeClaims.TryRemove(claim.MessageId, out _);
                        }
                    }
                }
                finally
                {
                    await renewalCancellation.CancelAsync();
                    await renewalTask;
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

    private async Task<IReadOnlyList<OutboxClaim>> ClaimBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GodForgeDbContext>();
        var now = _clock.UtcNow;

        if (!context.Database.IsRelational())
        {
            var inMemoryMessages = await context.OutboxMessages
                .Where(message =>
                    ((message.Status == OutboxMessageStatus.Pending || message.Status == OutboxMessageStatus.Failed) && message.AvailableAt <= now) ||
                    (message.Status == OutboxMessageStatus.Processing &&
                     ((message.LeaseExpiresAt != null && message.LeaseExpiresAt <= now) ||
                      (message.LeaseExpiresAt == null && message.AvailableAt <= now))))
                .OrderBy(message => message.CreatedAt)
                .Take(BatchSize)
                .ToListAsync(cancellationToken);

            var inMemoryClaims = new List<OutboxClaim>(inMemoryMessages.Count);
            foreach (var message in inMemoryMessages)
            {
                var leaseId = Guid.NewGuid();
                message.MarkProcessing(leaseId, now.Add(ClaimLease), now);
                inMemoryClaims.Add(new OutboxClaim(message.Id, leaseId));
            }

            await context.SaveChangesAsync(cancellationToken);
            return inMemoryClaims;
        }

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var messages = await context.OutboxMessages
            .FromSqlInterpolated($$"""
                SELECT *
                FROM ops.outbox_messages
                WHERE ((status IN ('pending', 'failed') AND available_at <= {{now}})
                       OR (status = 'processing' AND
                           ((lease_expires_at IS NOT NULL AND lease_expires_at <= {{now}})
                            OR (lease_expires_at IS NULL AND available_at <= {{now}}))))
                ORDER BY created_at
                FOR UPDATE SKIP LOCKED
                LIMIT {{BatchSize}}
                """)
            .ToListAsync(cancellationToken);

        var claims = new List<OutboxClaim>(messages.Count);
        foreach (var message in messages)
        {
            var leaseId = Guid.NewGuid();
            message.MarkProcessing(leaseId, now.Add(ClaimLease), now);
            claims.Add(new OutboxClaim(message.Id, leaseId));
        }

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return claims;
    }

    private async Task RenewClaimBatchAsync(
        ConcurrentDictionary<Guid, OutboxClaim> claims,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(LeaseRenewalInterval, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            foreach (var claim in claims.Values)
            {
                try
                {
                    if (!await RenewClaimAsync(claim, cancellationToken))
                    {
                        _logger.LogWarning(
                            "Outbox lease {LeaseId} for message {OutboxMessageId} is no longer owned by this dispatcher",
                            claim.LeaseId,
                            claim.MessageId);
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(
                        exception,
                        "Could not renew outbox lease {LeaseId} for message {OutboxMessageId}",
                        claim.LeaseId,
                        claim.MessageId);
                }
            }
        }
    }

    private async Task<bool> RenewClaimAsync(OutboxClaim claim, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GodForgeDbContext>();
        var now = _clock.UtcNow;
        var leaseExpiresAt = now.Add(ClaimLease);

        if (context.Database.IsRelational())
        {
            var affected = await context.OutboxMessages
                .Where(message =>
                    message.Id == claim.MessageId &&
                    message.Status == OutboxMessageStatus.Processing &&
                    message.LeaseId == claim.LeaseId)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(message => message.AvailableAt, leaseExpiresAt)
                        .SetProperty(message => message.LeaseExpiresAt, leaseExpiresAt),
                    cancellationToken);
            return affected == 1;
        }

        var message = await context.OutboxMessages
            .FirstOrDefaultAsync(item => item.Id == claim.MessageId, cancellationToken);
        if (message is null || !message.IsOwnedBy(claim.LeaseId))
            return false;

        message.RenewLease(claim.LeaseId, leaseExpiresAt, now);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task DispatchAsync(OutboxClaim claim, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GodForgeDbContext>();
        var message = await context.OutboxMessages
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item =>
                    item.Id == claim.MessageId &&
                    item.Status == OutboxMessageStatus.Processing &&
                    item.LeaseId == claim.LeaseId,
                cancellationToken);

        if (message is null)
            return;

        try
        {
            if (message.EventType == RetiredProviderReconciliationEventType)
            {
                _logger.LogWarning(
                    "Retiring legacy provider reconciliation outbox message {OutboxMessageId}; no provider membership adapter exists in this codebase.",
                    message.Id);
            }
            else if (message.EventType == EmailOutbox.EventType)
            {
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                var encryptionSettings = scope.ServiceProvider.GetRequiredService<IOptions<OutboxEncryptionSettings>>().Value;
                var payload = EmailOutbox.Decrypt(
                    message.PayloadJson,
                    encryptionSettings.Key,
                    encryptionSettings.LegacyKey);
                await emailService.SendEmailAsync(payload.Recipient, payload.Subject, payload.HtmlBody, cancellationToken);
            }
            else
            {
                var publisher = scope.ServiceProvider.GetRequiredService<IJobPublisher>();
                var workerMessage = DeserializeMessage(message.EventType, message.PayloadJson);
                await publisher.PublishAsync(message.EventType, workerMessage, cancellationToken);
            }

            if (!await MarkProcessedAsync(claim, cancellationToken))
            {
                _logger.LogWarning(
                    "Outbox message {OutboxMessageId} was delivered but its lease {LeaseId} was lost before completion could be recorded",
                    claim.MessageId,
                    claim.LeaseId);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            var errorMessage = SanitizeError(exception.Message);
            var nextAttempt = message.Attempts + 1;
            var shouldDeadLetter = nextAttempt >= _settings.MaxAttempts;
            var now = _clock.UtcNow;
            var delaySeconds = Math.Min(300, Math.Pow(2, Math.Min(message.Attempts, 6)) * 5);
            var nextAvailableAt = shouldDeadLetter ? now : now.AddSeconds(delaySeconds);
            var outcome = await RecordFailureAsync(
                claim,
                message,
                errorMessage,
                nextAvailableAt,
                shouldDeadLetter,
                cancellationToken);

            if (outcome == FailureRecordOutcome.RetryScheduled)
            {
                _logger.LogWarning(
                    exception,
                    "Outbox message {OutboxMessageId} could not be dispatched and was scheduled for attempt {AttemptNumber} of {MaxAttempts}",
                    message.Id,
                    nextAttempt + 1,
                    _settings.MaxAttempts);
            }
            else if (outcome == FailureRecordOutcome.DeadLettered)
            {
                _logger.LogError(
                    exception,
                    "Outbox message {OutboxMessageId} was dead-lettered after {AttemptCount} failed dispatch attempts",
                    message.Id,
                    nextAttempt);
            }
            else
            {
                _logger.LogWarning(
                    exception,
                    "Outbox message {OutboxMessageId} failed after lease ownership had already changed",
                    message.Id);
            }
        }
    }

    private async Task<bool> MarkProcessedAsync(OutboxClaim claim, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GodForgeDbContext>();
        var now = _clock.UtcNow;

        if (context.Database.IsRelational())
        {
            var affected = await context.OutboxMessages
                .Where(message =>
                    message.Id == claim.MessageId &&
                    message.Status == OutboxMessageStatus.Processing &&
                    message.LeaseId == claim.LeaseId)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(message => message.Status, OutboxMessageStatus.Processed)
                        .SetProperty(message => message.ProcessedAt, now)
                        .SetProperty(message => message.ErrorMessage, (string?)null)
                        .SetProperty(message => message.LeaseId, (Guid?)null)
                        .SetProperty(message => message.LeaseExpiresAt, (DateTimeOffset?)null),
                    cancellationToken);
            return affected == 1;
        }

        var message = await context.OutboxMessages
            .FirstOrDefaultAsync(item => item.Id == claim.MessageId, cancellationToken);
        if (message is null || !message.IsOwnedBy(claim.LeaseId))
            return false;

        message.MarkAsProcessed(claim.LeaseId, now);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<FailureRecordOutcome> RecordFailureAsync(
        OutboxClaim claim,
        OutboxMessage sourceMessage,
        string errorMessage,
        DateTimeOffset nextAvailableAt,
        bool shouldDeadLetter,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GodForgeDbContext>();
        var now = _clock.UtcNow;

        if (context.Database.IsRelational())
        {
            if (shouldDeadLetter)
            {
                await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
                var deadLetterAffected = await context.OutboxMessages
                    .Where(message =>
                        message.Id == claim.MessageId &&
                        message.Status == OutboxMessageStatus.Processing &&
                        message.LeaseId == claim.LeaseId)
                    .ExecuteUpdateAsync(
                        setters => setters
                            .SetProperty(message => message.Attempts, message => message.Attempts + 1)
                            .SetProperty(message => message.Status, OutboxMessageStatus.DeadLettered)
                            .SetProperty(message => message.ErrorMessage, errorMessage)
                            .SetProperty(message => message.AvailableAt, now)
                            .SetProperty(message => message.LeaseId, (Guid?)null)
                            .SetProperty(message => message.LeaseExpiresAt, (DateTimeOffset?)null),
                        cancellationToken);

                if (deadLetterAffected != 1)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return FailureRecordOutcome.NotRecorded;
                }

                context.DeadLetterMessages.Add(CreateDeadLetterMessage(
                    sourceMessage,
                    errorMessage,
                    sourceMessage.Attempts + 1,
                    now));
                await context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return FailureRecordOutcome.DeadLettered;
            }

            var affected = await context.OutboxMessages
                .Where(message =>
                    message.Id == claim.MessageId &&
                    message.Status == OutboxMessageStatus.Processing &&
                    message.LeaseId == claim.LeaseId)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(message => message.Attempts, message => message.Attempts + 1)
                        .SetProperty(message => message.Status, OutboxMessageStatus.Failed)
                        .SetProperty(message => message.ErrorMessage, errorMessage)
                        .SetProperty(message => message.AvailableAt, nextAvailableAt)
                        .SetProperty(message => message.LeaseId, (Guid?)null)
                        .SetProperty(message => message.LeaseExpiresAt, (DateTimeOffset?)null),
                    cancellationToken);
            return affected == 1
                ? FailureRecordOutcome.RetryScheduled
                : FailureRecordOutcome.NotRecorded;
        }

        var message = await context.OutboxMessages
            .FirstOrDefaultAsync(item => item.Id == claim.MessageId, cancellationToken);
        if (message is null || !message.IsOwnedBy(claim.LeaseId))
            return FailureRecordOutcome.NotRecorded;

        if (shouldDeadLetter)
        {
            message.MarkDeadLettered(claim.LeaseId, errorMessage, now);
            context.DeadLetterMessages.Add(CreateDeadLetterMessage(
                sourceMessage,
                errorMessage,
                message.Attempts,
                now));
        }
        else
        {
            message.RecordAttempt(claim.LeaseId, errorMessage, nextAvailableAt, now);
        }

        await context.SaveChangesAsync(cancellationToken);
        return shouldDeadLetter
            ? FailureRecordOutcome.DeadLettered
            : FailureRecordOutcome.RetryScheduled;
    }

    private static DeadLetterMessage CreateDeadLetterMessage(
        OutboxMessage message,
        string errorMessage,
        int attemptCount,
        DateTimeOffset now)
    {
        var detailsJson = JsonSerializer.Serialize(new
        {
            outboxMessageId = message.Id,
            message.AggregateType,
            message.AggregateId,
            message.EventType,
            message.CorrelationId,
            attemptCount
        }, JsonOptions);

        return DeadLetterMessage.Create(
            NormalizeQueueName(message.EventType),
            message.Id.ToString("D"),
            message.PayloadJson,
            $"Outbox dispatch failed after {attemptCount} attempts: {errorMessage}",
            detailsJson,
            now);
    }

    private static string NormalizeQueueName(string eventType)
    {
        const int maxLength = 100;
        return eventType.Length <= maxLength ? eventType : eventType[..maxLength];
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

    private enum FailureRecordOutcome
    {
        NotRecorded,
        RetryScheduled,
        DeadLettered
    }

    private sealed record OutboxClaim(Guid MessageId, Guid LeaseId);
}
