using FluentValidation;
using GodForge.Application.Common.Models;
using GodForge.Application.Features.Projects.DTOs;
using MediatR;

namespace GodForge.Application.Features.Projects.Queries.ListProjects;

public sealed record ListProjectsQuery(
    Guid ActorId,
    int Page,
    int PageSize,
    Guid? OrganizationId,
    string? Status,
    string? Search) : IRequest<Result<PagedResult<ProjectDto>>>;

public sealed class ListProjectsQueryHandler : IRequestHandler<ListProjectsQuery, Result<PagedResult<ProjectDto>>>
{
    private readonly IProjectManagementService _projects;

    public ListProjectsQueryHandler(IProjectManagementService projects) => _projects = projects;

    public Task<Result<PagedResult<ProjectDto>>> Handle(ListProjectsQuery request, CancellationToken cancellationToken)
        => _projects.ListAsync(
            request.ActorId,
            request.Page,
            request.PageSize,
            request.OrganizationId,
            request.Status,
            request.Search,
            cancellationToken);
}

public sealed class ListProjectsQueryValidator : AbstractValidator<ListProjectsQuery>
{
    public ListProjectsQueryValidator()
    {
        RuleFor(request => request.ActorId).NotEmpty();
        RuleFor(request => request.Page).GreaterThan(0);
        RuleFor(request => request.PageSize).InclusiveBetween(1, 100);
        RuleFor(request => request.Status).MaximumLength(30);
        RuleFor(request => request.Search).MaximumLength(200);
    }
}
