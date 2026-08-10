using FluentValidation;
using GodForge.Application.Common.Models;
using GodForge.Application.Features.Projects.DTOs;
using MediatR;

namespace GodForge.Application.Features.Projects.Commands.RestoreProject;

public sealed record RestoreProjectCommand(Guid ActorId, Guid ProjectId, long Version) : IRequest<Result<ProjectDto>>;

public sealed class RestoreProjectCommandHandler : IRequestHandler<RestoreProjectCommand, Result<ProjectDto>>
{
    private readonly IProjectManagementService _projects;

    public RestoreProjectCommandHandler(IProjectManagementService projects) => _projects = projects;

    public Task<Result<ProjectDto>> Handle(RestoreProjectCommand request, CancellationToken cancellationToken)
        => _projects.RestoreAsync(request.ActorId, request.ProjectId, request.Version, cancellationToken);
}

public sealed class RestoreProjectCommandValidator : AbstractValidator<RestoreProjectCommand>
{
    public RestoreProjectCommandValidator()
    {
        RuleFor(request => request.ActorId).NotEmpty();
        RuleFor(request => request.ProjectId).NotEmpty();
        RuleFor(request => request.Version).GreaterThan(0);
    }
}
