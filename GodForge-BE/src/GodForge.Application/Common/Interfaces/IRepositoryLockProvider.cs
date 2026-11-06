namespace GodForge.Application.Common.Interfaces;

public interface IRepositoryLockProvider
{
    Task<IAsyncDisposable> AcquireOperationAsync(Guid repositoryId, TimeSpan timeout, CancellationToken cancellationToken = default);
    Task<IAsyncDisposable> AcquireAsync(
        Guid repositoryId,
        TimeSpan timeout,
        CancellationToken cancellationToken = default);
}
