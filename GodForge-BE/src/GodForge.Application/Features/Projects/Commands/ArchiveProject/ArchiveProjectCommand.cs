using FluentValidation;
using GodForge.Application.Common.Models;
using GodForge.Application.Features.Projects.DTOs;
using MediatR;

namespace GodForge.Application.Features.Projects.Commands.ArchiveProject;

public sealed record ArchiveProjectCommand(Guid ActorId, Guid ProjectId, long Version) : IRequest<Result<ProjectDto>>;

public sealed class ArchiveProjectCommandHandler(IProjectLifecycleService projects) : IRequestHandler<ArchiveProjectCommand, Result<ProjectDto>>
{
    public Task<Result<ProjectDto>> Handle(ArchiveProjectCommand request, CancellationToken cancellationToken)
        => projects.ArchiveAsync(request.ActorId, request.ProjectId, request.Version, cancellationToken);
}

public sealed class ArchiveProjectCommandValidator : AbstractValidator<ArchiveProjectCommand>
{
    public ArchiveProjectCommandValidator()
    {
        RuleFor(request => request.ActorId).NotEmpty();
        RuleFor(request => request.ProjectId).NotEmpty();
        RuleFor(request => request.Version).GreaterThan(0);
    }
}
