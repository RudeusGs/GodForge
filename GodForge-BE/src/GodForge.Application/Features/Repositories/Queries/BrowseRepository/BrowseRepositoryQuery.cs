using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models;
using GodForge.Application.Common.Security;
using GodForge.Domain.Enums;
using MediatR;

namespace GodForge.Application.Features.Repositories.Queries.BrowseRepository;

public enum RepositoryBrowseOperation { Branches, Commits, Tree, Blob }
public sealed record BrowseRepositoryQuery(Guid ProjectId, Guid ActorId, RepositoryBrowseOperation Operation,
    string? Branch = null, string? CommitSha = null, string Path = "", int Page = 1, int PageSize = 20) : IRequest<Result<object>>;

public sealed class BrowseRepositoryQueryHandler(IGitRepositoryRepository repositories, IAuthorizationService authorization,
    IHostedGitService provider, ILinkedRepositoryBrowser linked) : IRequestHandler<BrowseRepositoryQuery, Result<object>>
{
    public async Task<Result<object>> Handle(BrowseRepositoryQuery request, CancellationToken cancellationToken)
    {
        if (!await authorization.HasPermissionAsync(request.ActorId, request.ProjectId, Permissions.RepositoryRead, cancellationToken))
            return ApplicationError.NotFound("REPOSITORY_NOT_CONNECTED", "Repository was not found.");
        if (request.Page is < 1 or > 10000 || request.PageSize is < 1 or > 100)
            return ApplicationError.Validation("VALIDATION_ERROR", "Page must be 1–10000 and pageSize 1–100.");
        var repository = await repositories.GetByProjectIdAsync(request.ProjectId, cancellationToken);
        if (repository is null) return ApplicationError.NotFound("REPOSITORY_NOT_CONNECTED", "Repository was not found.");
        if (repository.Mode == RepositoryMode.InternalHosted && repository.HostedRepositoryId is null || repository.GitRepositoryStatus == GitRepositoryStatus.Disabled)
            return ApplicationError.Conflict("REPOSITORY_PROVISIONING", "Repository is not available for browsing.");
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(30));
            var token = timeout.Token;
            if (repository.Mode == RepositoryMode.ExternalLinked)
            {
                object external = request.Operation switch
                {
                    RepositoryBrowseOperation.Branches => await linked.BranchesAsync(repository.Id, repository.DefaultBranch, request.Page, request.PageSize, token),
                    RepositoryBrowseOperation.Commits => await linked.CommitsAsync(repository.Id, request.Branch ?? repository.DefaultBranch, request.Page, request.PageSize, token),
                    RepositoryBrowseOperation.Tree => await linked.TreeAsync(repository.Id, request.CommitSha ?? "", request.Path, token),
                    RepositoryBrowseOperation.Blob => await linked.BlobAsync(repository.Id, request.CommitSha ?? "", request.Path, token),
                    _ => throw new HostedGitException("VALIDATION_ERROR")
                };
                return Result<object>.Success(external);
            }
            var providerId = repository.HostedRepositoryId ?? throw new HostedGitException("REPOSITORY_NOT_CONNECTED");
            object result = request.Operation switch
            {
                RepositoryBrowseOperation.Branches => await provider.BranchesAsync(providerId, request.Page, request.PageSize, token),
                RepositoryBrowseOperation.Commits => await provider.CommitsAsync(providerId, request.Branch ?? repository.DefaultBranch, request.Page, request.PageSize, token),
                RepositoryBrowseOperation.Tree => await provider.TreeAsync(providerId, request.CommitSha ?? "", request.Path, token),
                RepositoryBrowseOperation.Blob => await provider.BlobAsync(providerId, request.CommitSha ?? "", request.Path, token),
                _ => throw new HostedGitException("VALIDATION_ERROR")
            };
            return Result<object>.Success(result);
        }
        catch (HostedGitException exception)
        {
            return exception.Code switch
            {
                "REPOSITORY_PROVIDER_NOT_FOUND" => ApplicationError.NotFound(exception.Code, "Repository or revision was not found."),
                "REPOSITORY_PATH_INVALID" or "REPOSITORY_REF_INVALID" or "REPOSITORY_BINARY_FILE" or "REPOSITORY_BLOB_TOO_LARGE" or "REPOSITORY_TREE_TOO_LARGE" =>
                    ApplicationError.Validation(exception.Code, "The requested repository content cannot be displayed."),
                _ => ApplicationError.Conflict(exception.Code, "Hosted Git is currently unavailable for this operation.")
            };
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        { return ApplicationError.Conflict("FORGEJO_UNAVAILABLE", "Hosted Git request timed out."); }
        catch (Exception e) when (e is KeyNotFoundException or InvalidOperationException or FormatException)
        { return ApplicationError.Conflict("FORGEJO_RESPONSE_INVALID", "Hosted Git returned an invalid response."); }
    }
}
