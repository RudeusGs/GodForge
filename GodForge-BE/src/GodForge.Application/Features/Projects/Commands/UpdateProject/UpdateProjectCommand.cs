using FluentValidation;
using GodForge.Application.Common.Models;
using GodForge.Application.Features.Projects.DTOs;
using GodForge.Domain.Entities.Core;
using MediatR;

namespace GodForge.Application.Features.Projects.Commands.UpdateProject;

public sealed record UpdateProjectCommand(
    Guid ActorId,
    Guid ProjectId,
    string Name,
    string Slug,
    string? Description,
    string Visibility,
    long Version) : IRequest<Result<ProjectDto>>;

public sealed class UpdateProjectCommandHandler : IRequestHandler<UpdateProjectCommand, Result<ProjectDto>>
{
    private readonly IProjectManagementService _projects;

    public UpdateProjectCommandHandler(IProjectManagementService projects) => _projects = projects;

    public Task<Result<ProjectDto>> Handle(UpdateProjectCommand request, CancellationToken cancellationToken)
        => _projects.UpdateAsync(
            request.ActorId,
            request.ProjectId,
            request.Name,
            request.Slug,
            request.Description,
            request.Visibility,
            request.Version,
            cancellationToken);
}

public sealed class UpdateProjectCommandValidator : AbstractValidator<UpdateProjectCommand>
{
    public UpdateProjectCommandValidator()
    {
        RuleFor(request => request.ActorId).NotEmpty();
        RuleFor(request => request.ProjectId).NotEmpty();
        RuleFor(request => request.Name).NotEmpty().MaximumLength(Project.MaxNameLength);
        RuleFor(request => request.Slug).NotEmpty().MaximumLength(Project.MaxSlugLength);
        RuleFor(request => request.Visibility).NotEmpty().MaximumLength(30);
        RuleFor(request => request.Version).GreaterThan(0);
    }
}
