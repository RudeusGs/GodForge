using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models;
using GodForge.Application.Common.Security;
using GodForge.Application.Features.Repositories.DTOs;
using MediatR;

namespace GodForge.Application.Features.Repositories.Queries.GetRepository;

public sealed class GetRepositoryQueryHandler : IRequestHandler<GetRepositoryQuery, Result<RepositoryDto>>
{
    private readonly IGitRepositoryRepository _repositories;
    private readonly IAuthorizationService _authorization;

    public GetRepositoryQueryHandler(
        IGitRepositoryRepository repositories,
        IAuthorizationService authorization)
    {
        _repositories = repositories;
        _authorization = authorization;
    }

    public async Task<Result<RepositoryDto>> Handle(GetRepositoryQuery request, CancellationToken cancellationToken)
    {
        if (!await _authorization.HasPermissionAsync(
                request.ActorId,
                request.ProjectId,
                Permissions.RepositoryRead,
                cancellationToken))
        {
            return ApplicationError.Forbidden("SECURITY_FORBIDDEN", "You do not have access to this repository.");
        }

        var repository = await _repositories.GetByProjectIdAsync(request.ProjectId, cancellationToken);
        return repository is null
            ? ApplicationError.NotFound("REPOSITORY_NOT_CONNECTED", "No repository is connected to this project.")
            : RepositoryDto.From(repository);
    }
}
