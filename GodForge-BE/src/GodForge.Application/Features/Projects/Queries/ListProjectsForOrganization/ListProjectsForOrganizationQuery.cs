using FluentValidation;
using GodForge.Application.Common.Models;
using GodForge.Application.Features.Projects.DTOs;
using MediatR;

namespace GodForge.Application.Features.Projects.Queries.ListProjectsForOrganization;

public sealed record ListProjectsForOrganizationQuery(
    Guid ActorId,
    Guid OrganizationId,
    int Page,
    int PageSize) : IRequest<Result<PagedResult<ProjectAdministrationDto>>>;

public sealed class ListProjectsForOrganizationQueryHandler : IRequestHandler<ListProjectsForOrganizationQuery, Result<PagedResult<ProjectAdministrationDto>>>
{
    private readonly IProjectManagementService _projects;

    public ListProjectsForOrganizationQueryHandler(IProjectManagementService projects) => _projects = projects;

    public Task<Result<PagedResult<ProjectAdministrationDto>>> Handle(
        ListProjectsForOrganizationQuery request,
        CancellationToken cancellationToken)
        => _projects.ListForOrganizationAsync(
            request.ActorId,
            request.OrganizationId,
            request.Page,
            request.PageSize,
            cancellationToken);
}

public sealed class ListProjectsForOrganizationQueryValidator : AbstractValidator<ListProjectsForOrganizationQuery>
{
    public ListProjectsForOrganizationQueryValidator()
    {
        RuleFor(request => request.ActorId).NotEmpty();
        RuleFor(request => request.OrganizationId).NotEmpty();
        RuleFor(request => request.Page).GreaterThan(0);
        RuleFor(request => request.PageSize).InclusiveBetween(1, 100);
    }
}
