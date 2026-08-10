using FluentValidation;
using GodForge.Application.Common.Models;
using GodForge.Application.Features.Projects.DTOs;
using MediatR;

namespace GodForge.Application.Features.Projects.Queries.ListProjectMembers;

public sealed record ListProjectMembersQuery(
    Guid ActorId,
    Guid ProjectId,
    int Page,
    int PageSize,
    string? Role,
    string? Status,
    string? Search) : IRequest<Result<PagedResult<ProjectMemberDto>>>;

public sealed class ListProjectMembersQueryHandler : IRequestHandler<ListProjectMembersQuery, Result<PagedResult<ProjectMemberDto>>>
{
    private readonly IProjectManagementService _projects;

    public ListProjectMembersQueryHandler(IProjectManagementService projects) => _projects = projects;

    public Task<Result<PagedResult<ProjectMemberDto>>> Handle(
        ListProjectMembersQuery request,
        CancellationToken cancellationToken)
        => _projects.ListMembersAsync(
            request.ActorId,
            request.ProjectId,
            request.Page,
            request.PageSize,
            request.Role,
            request.Status,
            request.Search,
            cancellationToken);
}

public sealed class ListProjectMembersQueryValidator : AbstractValidator<ListProjectMembersQuery>
{
    public ListProjectMembersQueryValidator()
    {
        RuleFor(request => request.ActorId).NotEmpty();
        RuleFor(request => request.ProjectId).NotEmpty();
        RuleFor(request => request.Page).GreaterThan(0);
        RuleFor(request => request.PageSize).InclusiveBetween(1, 100);
        RuleFor(request => request.Role).MaximumLength(30);
        RuleFor(request => request.Status).MaximumLength(30);
        RuleFor(request => request.Search).MaximumLength(200);
    }
}
