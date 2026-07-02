using GodForge.Domain.Enums;

namespace GodForge.Application.Common.Interfaces;

public interface IJobPublisher
{
    Task PublishAsync(
        Guid jobId,
        Guid projectId,
        JobType type,
        string correlationId,
        string? inputHash,
        CancellationToken cancellationToken = default);
}
