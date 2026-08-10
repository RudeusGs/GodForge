using FluentValidation;
using GodForge.Application.Common.Models;
using GodForge.Application.Features.Projects.DTOs;
using GodForge.Domain.Entities.Core;
using MediatR;

namespace GodForge.Application.Features.Projects.Commands.RequestProjectDeletion;

public sealed record RequestProjectDeletionCommand(
    Guid ActorId,
    Guid ProjectId,
    long Version,
    string ConfirmationSlug) : IRequest<Result<ProjectDeletionAcceptedDto>>;

public sealed class RequestProjectDeletionCommandHandler : IRequestHandler<RequestProjectDeletionCommand, Result<ProjectDeletionAcceptedDto>>
{
    private readonly IProjectManagementService _projects;

    public RequestProjectDeletionCommandHandler(IProjectManagementService projects) => _projects = projects;

    public Task<Result<ProjectDeletionAcceptedDto>> Handle(
        RequestProjectDeletionCommand request,
        CancellationToken cancellationToken)
        => _projects.RequestDeletionAsync(
            request.ActorId,
            request.ProjectId,
            request.Version,
            request.ConfirmationSlug,
            cancellationToken);
}

public sealed class RequestProjectDeletionCommandValidator : AbstractValidator<RequestProjectDeletionCommand>
{
    public RequestProjectDeletionCommandValidator()
    {
        RuleFor(request => request.ActorId).NotEmpty();
        RuleFor(request => request.ProjectId).NotEmpty();
        RuleFor(request => request.Version).GreaterThan(0);
        RuleFor(request => request.ConfirmationSlug).NotEmpty().MaximumLength(Project.MaxSlugLength);
    }
}
