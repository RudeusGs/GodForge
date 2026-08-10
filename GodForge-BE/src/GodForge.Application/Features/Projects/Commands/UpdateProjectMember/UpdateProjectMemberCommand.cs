using FluentValidation;
using GodForge.Application.Common.Models;
using GodForge.Application.Features.Projects.DTOs;
using MediatR;

namespace GodForge.Application.Features.Projects.Commands.UpdateProjectMember;

public sealed record UpdateProjectMemberCommand(
    Guid ActorId,
    Guid ProjectId,
    Guid UserId,
    string Role,
    long Version) : IRequest<Result<ProjectMemberDto>>;

public sealed class UpdateProjectMemberCommandHandler : IRequestHandler<UpdateProjectMemberCommand, Result<ProjectMemberDto>>
{
    private readonly IProjectManagementService _projects;

    public UpdateProjectMemberCommandHandler(IProjectManagementService projects) => _projects = projects;

    public Task<Result<ProjectMemberDto>> Handle(UpdateProjectMemberCommand request, CancellationToken cancellationToken)
        => _projects.UpdateMemberAsync(
            request.ActorId,
            request.ProjectId,
            request.UserId,
            request.Role,
            request.Version,
            cancellationToken);
}

public sealed class UpdateProjectMemberCommandValidator : AbstractValidator<UpdateProjectMemberCommand>
{
    public UpdateProjectMemberCommandValidator()
    {
        RuleFor(request => request.ActorId).NotEmpty();
        RuleFor(request => request.ProjectId).NotEmpty();
        RuleFor(request => request.UserId).NotEmpty();
        RuleFor(request => request.Role).NotEmpty().MaximumLength(30);
        RuleFor(request => request.Version).GreaterThan(0);
    }
}
