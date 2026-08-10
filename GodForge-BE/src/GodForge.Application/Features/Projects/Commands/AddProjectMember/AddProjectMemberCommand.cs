using FluentValidation;
using GodForge.Application.Common.Models;
using GodForge.Application.Features.Projects.DTOs;
using MediatR;

namespace GodForge.Application.Features.Projects.Commands.AddProjectMember;

public sealed record AddProjectMemberCommand(
    Guid ActorId,
    Guid ProjectId,
    Guid UserId,
    string Role) : IRequest<Result<ProjectMemberDto>>;

public sealed class AddProjectMemberCommandHandler : IRequestHandler<AddProjectMemberCommand, Result<ProjectMemberDto>>
{
    private readonly IProjectManagementService _projects;

    public AddProjectMemberCommandHandler(IProjectManagementService projects) => _projects = projects;

    public Task<Result<ProjectMemberDto>> Handle(AddProjectMemberCommand request, CancellationToken cancellationToken)
        => _projects.AddMemberAsync(
            request.ActorId,
            request.ProjectId,
            request.UserId,
            request.Role,
            cancellationToken);
}

public sealed class AddProjectMemberCommandValidator : AbstractValidator<AddProjectMemberCommand>
{
    public AddProjectMemberCommandValidator()
    {
        RuleFor(request => request.ActorId).NotEmpty();
        RuleFor(request => request.ProjectId).NotEmpty();
        RuleFor(request => request.UserId).NotEmpty();
        RuleFor(request => request.Role).NotEmpty().MaximumLength(30);
    }
}
