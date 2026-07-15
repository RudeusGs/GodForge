using GodForge.Application.Common.Models.Messages;

namespace GodForge.Application.Common.Interfaces;

public interface IJobPublisher
{
    Task PublishAsync(
        string queueName,
        WorkerMessage message,
        CancellationToken cancellationToken = default);
}
