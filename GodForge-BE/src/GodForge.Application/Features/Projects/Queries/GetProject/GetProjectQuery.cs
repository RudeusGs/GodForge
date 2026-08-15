using FluentValidation;
using GodForge.Application.Common.Models;
using GodForge.Application.Features.Projects.DTOs;
using MediatR;

namespace GodForge.Application.Features.Projects.Queries.GetProject;

public sealed record GetProjectQuery(Guid ActorId, Guid ProjectId) : IRequest<Result<ProjectDto>>;

public sealed class GetProjectQueryHandler : IRequestHandler<GetProjectQuery, Result<ProjectDto>>
{
    private readonly IProjectLifecycleService _projects;

    public GetProjectQueryHandler(IProjectLifecycleService projects) => _projects = projects;

    public Task<Result<ProjectDto>> Handle(GetProjectQuery request, CancellationToken cancellationToken)
        => _projects.GetAsync(request.ActorId, request.ProjectId, cancellationToken);
}

public sealed class GetProjectQueryValidator : AbstractValidator<GetProjectQuery>
{
    public GetProjectQueryValidator()
    {
        RuleFor(request => request.ActorId).NotEmpty();
        RuleFor(request => request.ProjectId).NotEmpty();
    }
}
