using FluentValidation;
using GodForge.Application.Common.Models;
using GodForge.Application.Features.Projects.DTOs;
using MediatR;

namespace GodForge.Application.Features.Projects.Queries.GetProjectSettings;

public sealed record GetProjectSettingsQuery(Guid ActorId, Guid ProjectId) : IRequest<Result<ProjectSettingsDto>>;

public sealed class GetProjectSettingsQueryHandler : IRequestHandler<GetProjectSettingsQuery, Result<ProjectSettingsDto>>
{
    private readonly IProjectManagementService _projects;

    public GetProjectSettingsQueryHandler(IProjectManagementService projects) => _projects = projects;

    public Task<Result<ProjectSettingsDto>> Handle(GetProjectSettingsQuery request, CancellationToken cancellationToken)
        => _projects.GetSettingsAsync(request.ActorId, request.ProjectId, cancellationToken);
}

public sealed class GetProjectSettingsQueryValidator : AbstractValidator<GetProjectSettingsQuery>
{
    public GetProjectSettingsQueryValidator()
    {
        RuleFor(request => request.ActorId).NotEmpty();
        RuleFor(request => request.ProjectId).NotEmpty();
    }
}
