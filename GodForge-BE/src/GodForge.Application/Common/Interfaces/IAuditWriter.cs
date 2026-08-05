namespace GodForge.Application.Common.Interfaces;

public interface IAuditWriter
{
    Task WriteAuditAsync(
        Guid? actorUserId,
        Guid? projectId,
        string eventType,
        string? resourceType,
        Guid? resourceId,
        string outcome,
        object? details = null,
        CancellationToken cancellationToken = default);

    Task WriteSecurityAsync(
        Guid? userId,
        string eventType,
        string severity,
        object? details = null,
        CancellationToken cancellationToken = default);
}
