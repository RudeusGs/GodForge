using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models;
using GodForge.Application.Common.Security;
using GodForge.Application.Features.Repositories.DTOs;
using GodForge.Domain.Entities.Repo;
using GodForge.Domain.Enums;
using MediatR;

namespace GodForge.Application.Features.Repositories.Commands.LinkRepository;

public sealed class LinkRepositoryCommandHandler : IRequestHandler<LinkRepositoryCommand, Result<RepositoryDto>>
{
    private readonly IGitRepositoryRepository _repositories;
    private readonly IAuthorizationService _authorization;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IActivityWriter _activityWriter;
    private readonly IClock _clock;

    public LinkRepositoryCommandHandler(
        IGitRepositoryRepository repositories,
        IAuthorizationService authorization,
        IUnitOfWork unitOfWork,
        IActivityWriter activityWriter,
        IClock clock)
    {
        _repositories = repositories;
        _authorization = authorization;
        _unitOfWork = unitOfWork;
        _activityWriter = activityWriter;
        _clock = clock;
    }

    public async Task<Result<RepositoryDto>> Handle(
        LinkRepositoryCommand request,
        CancellationToken cancellationToken)
    {
        if (!await _authorization.HasPermissionAsync(
                request.ActorId,
                request.ProjectId,
                Permissions.RepositoryManage,
                cancellationToken))
        {
            return ApplicationError.Forbidden("SECURITY_FORBIDDEN", "You do not have permission to configure this repository.");
        }

        if (await _repositories.GetByProjectIdAsync(request.ProjectId, cancellationToken) is not null)
        {
            return ApplicationError.Conflict("REPOSITORY_ALREADY_CONNECTED", "A repository is already connected to this project.");
        }

        if (!Enum.TryParse<GitProvider>(request.Provider, true, out var provider) || provider == GitProvider.Forgejo)
        {
            return ApplicationError.Validation("REPOSITORY_PROVIDER_INVALID", "Use GitHub, GitLab, Bitbucket, or Generic for an external linked repository.");
        }

        var now = _clock.UtcNow;
        var repository = GitRepository.CreateLinked(
            request.ProjectId,
            request.RemoteUrl,
            provider,
            request.DefaultBranch,
            request.ExternalRepositoryId,
            request.AutoAnalyzeEnabled,
            now);

        await _repositories.AddAsync(repository, cancellationToken);
        await _activityWriter.WriteAsync(
            request.ProjectId,
            request.ActorId,
            "repository.linked",
            "repository",
            repository.Id.ToString(),
            ActivityStatus.Succeeded,
            request.CorrelationId,
            null,
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return RepositoryDto.From(repository);
    }
}
