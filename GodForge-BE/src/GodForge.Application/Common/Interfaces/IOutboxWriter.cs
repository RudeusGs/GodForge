using GodForge.Application.Common.Models.Messages;

namespace GodForge.Application.Common.Interfaces;

public interface IOutboxWriter
{
    Task EnqueueAsync(
        string queueName,
        WorkerMessage message,
        CancellationToken cancellationToken = default);

    Task EnqueueScheduledAsync(
        string queueName,
        WorkerMessage message,
        DateTimeOffset availableAt,
        CancellationToken cancellationToken = default);
}
