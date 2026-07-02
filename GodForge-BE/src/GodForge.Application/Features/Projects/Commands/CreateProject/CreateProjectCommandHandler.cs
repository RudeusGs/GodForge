using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models;
using GodForge.Application.Common.Security;
using GodForge.Application.Features.Projects.DTOs;
using GodForge.Domain.Entities;
using GodForge.Domain.Entities.Core;
using GodForge.Domain.Enums;
using MediatR;

namespace GodForge.Application.Features.Projects.Commands.CreateProject;

public sealed class CreateProjectCommandHandler : IRequestHandler<CreateProjectCommand, Result<ProjectDto>>
{
    private readonly IProjectRepository _projects;
    private readonly IProjectMemberRepository _members;
    private readonly IAuthorizationService _authorization;
    private readonly IActivityWriter _activityWriter;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CreateProjectCommandHandler(
        IProjectRepository projects,
        IProjectMemberRepository members,
        IAuthorizationService authorization,
        IActivityWriter activityWriter,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _projects = projects;
        _members = members;
        _authorization = authorization;
        _activityWriter = activityWriter;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<ProjectDto>> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
    {
        // System user check: only active users or system admins can create. Assuming standard user has the right.
        // Actually, the API controller requires authentication.

        if (await _projects.NameExistsAsync(request.ActorId, request.Name, cancellationToken))
            return ApplicationError.Conflict("PROJECT_NAME_EXISTS", "A project with this name already exists.");

        var now = _clock.UtcNow;
        var slug = request.Name.ToLowerInvariant().Replace(" ", "-");

        if (!Enum.TryParse<ProjectVisibility>(request.Visibility, true, out var visibility))
        {
            visibility = ProjectVisibility.Private;
        }

        var project = Project.Create(
            request.Name,
            slug,
            request.Description,
            "4.3", // Default godot version for now
            visibility,
            request.ActorId,
            now);

        var member = ProjectMember.Create(
            project.Id,
            request.ActorId,
            ProjectRole.ProjectOwner,
            ProjectMemberSource.Direct,
            request.ActorId,
            now);

        await _projects.AddAsync(project, cancellationToken);
        await _members.AddAsync(member, cancellationToken);

        await _activityWriter.WriteAsync(
            project.Id,
            request.ActorId,
            "project.created",
            "project",
            project.Id.ToString(),
            ActivityStatus.Succeeded,
            request.CorrelationId,
            null,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ProjectDto.From(project);
    }
}
