using FluentValidation;
using GodForge.Application.Common.Idempotency;
using GodForge.Application.Common.Models;
using GodForge.Application.Features.Projects.DTOs;
using GodForge.Domain.Entities.Core;
using MediatR;

namespace GodForge.Application.Features.Projects.Commands.CreateProject;

public sealed record CreateProjectCommand(
    Guid ActorId,
    Guid OrganizationId,
    string Name,
    string Slug,
    string? Description,
    string Visibility,
    string? IdempotencyKey) : IRequest<Result<ProjectDto>>;

public sealed class CreateProjectCommandHandler : IRequestHandler<CreateProjectCommand, Result<ProjectDto>>
{
    private readonly IProjectManagementService _projects;

    public CreateProjectCommandHandler(IProjectManagementService projects) => _projects = projects;

    public Task<Result<ProjectDto>> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
        => _projects.CreateAsync(
            request.ActorId,
            request.OrganizationId,
            request.Name,
            request.Slug,
            request.Description,
            request.Visibility,
            request.IdempotencyKey,
            cancellationToken);
}

public sealed class CreateProjectCommandValidator : AbstractValidator<CreateProjectCommand>
{
    public CreateProjectCommandValidator()
    {
        RuleFor(request => request.ActorId).NotEmpty();
        RuleFor(request => request.OrganizationId).NotEmpty();
        RuleFor(request => request.Name).NotEmpty().MaximumLength(Project.MaxNameLength);
        RuleFor(request => request.Slug).NotEmpty().MaximumLength(Project.MaxSlugLength);
        RuleFor(request => request.Visibility).NotEmpty().MaximumLength(30);
        RuleFor(request => request.IdempotencyKey).MaximumLength(IdempotencyRequest.MaxKeyLength);
    }
}
