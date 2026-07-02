using MediatR;
using GodForge.Application.Common.Models;
using GodForge.Application.Features.Projects.DTOs;

namespace GodForge.Application.Features.Projects.Queries.GetProjects;

public record GetProjectsQuery(
    Guid ActorId,
    int Page,
    int PageSize,
    string? Search) : IRequest<Result<PagedResult<ProjectDto>>>;
