using MediatR;
using GodForge.Application.Common.Models;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Features.Projects.DTOs;

namespace GodForge.Application.Features.Projects.Queries.GetProjects;

public sealed class GetProjectsQueryHandler : IRequestHandler<GetProjectsQuery, Result<PagedResult<ProjectDto>>>
{
    private readonly IProjectRepository _projects;

    public GetProjectsQueryHandler(IProjectRepository projects)
    {
        _projects = projects;
    }

    public async Task<Result<PagedResult<ProjectDto>>> Handle(GetProjectsQuery request, CancellationToken cancellationToken)
    {
        var pagedProjects = await _projects.GetVisibleProjectsAsync(
            request.ActorId,
            request.Page,
            request.PageSize,
            request.Search,
            cancellationToken);

        var dtos = pagedProjects.Items.Select(ProjectDto.From).ToList();

        var result = new PagedResult<ProjectDto>(
            dtos,
            pagedProjects.Page,
            pagedProjects.PageSize,
            pagedProjects.TotalItems);

        return Result<PagedResult<ProjectDto>>.Success(result);
    }
}
