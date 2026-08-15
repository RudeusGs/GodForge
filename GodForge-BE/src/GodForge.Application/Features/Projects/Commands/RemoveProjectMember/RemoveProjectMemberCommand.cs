using FluentValidation;
using GodForge.Application.Common.Models;
using MediatR;

namespace GodForge.Application.Features.Projects.Commands.RemoveProjectMember;

public sealed record RemoveProjectMemberCommand(
    Guid ActorId,
    Guid ProjectId,
    Guid UserId) : IRequest<Result>;

public sealed class RemoveProjectMemberCommandHandler : IRequestHandler<RemoveProjectMemberCommand, Result>
{
    private readonly IProjectMembershipService _projects;

    public RemoveProjectMemberCommandHandler(IProjectMembershipService projects) => _projects = projects;

    public Task<Result> Handle(RemoveProjectMemberCommand request, CancellationToken cancellationToken)
        => _projects.RemoveMemberAsync(request.ActorId, request.ProjectId, request.UserId, cancellationToken);
}

public sealed class RemoveProjectMemberCommandValidator : AbstractValidator<RemoveProjectMemberCommand>
{
    public RemoveProjectMemberCommandValidator()
    {
        RuleFor(request => request.ActorId).NotEmpty();
        RuleFor(request => request.ProjectId).NotEmpty();
        RuleFor(request => request.UserId).NotEmpty();
    }
}
