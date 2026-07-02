using GodForge.Domain.Enums;

namespace GodForge.Application.Common.Interfaces;

public interface IActivityWriter
{
    Task WriteAsync(
        Guid projectId,
        Guid? actorId,
        string action,
        string? targetType,
        string? targetId,
        ActivityStatus status,
        string correlationId,
        string? metadataJson = null,
        CancellationToken cancellationToken = default);
}
