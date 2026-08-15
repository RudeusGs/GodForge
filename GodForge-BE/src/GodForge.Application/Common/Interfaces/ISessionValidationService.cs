namespace GodForge.Application.Common.Interfaces;

public interface ISessionValidationService
{
    Task<bool> IsValidAsync(
        Guid sessionId,
        Guid userId,
        string securityStamp,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    Task InvalidateSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);

    Task InvalidateSessionsAsync(
        IReadOnlyCollection<Guid> sessionIds,
        CancellationToken cancellationToken = default);
}
