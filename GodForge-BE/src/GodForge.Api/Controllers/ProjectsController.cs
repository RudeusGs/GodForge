using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Models;
using GodForge.Application.Features.Activities.Queries.GetProjectActivities;
using GodForge.Application.Features.Jobs.Queries.GetProjectJobs;
using GodForge.Application.Features.Projects.Commands.CreateProject;
using GodForge.Application.Features.Projects.Queries.GetProjects;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GodForge.Api.Controllers;

[Authorize]
public class ProjectsController : BaseApiController
{
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;

    public ProjectsController(IMediator mediator, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Creates a new GodForge project.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<GodForge.Application.Features.Projects.DTOs.ProjectDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateProject([FromBody] CreateProjectRequest request, CancellationToken cancellationToken)
    {
        if (_currentUser.Id == null) return Unauthorized();

        var command = new CreateProjectCommand(
            request.Name,
            request.Description,
            request.Visibility,
            _currentUser.Id.Value,
            CorrelationId);

        Result<GodForge.Application.Features.Projects.DTOs.ProjectDto> result = await _mediator.Send(command, cancellationToken);
        if (result.IsSuccess)
        {
            return StatusCode(201, new ApiResponse<GodForge.Application.Features.Projects.DTOs.ProjectDto>
            {
                Data = result.Value,
                Meta = new ApiMeta { CorrelationId = CorrelationId }
            });
        }
        return HandleResult(result);
    }

    /// <summary>
    /// Retrieves a paginated list of projects accessible to the current user.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiPagedResponse<GodForge.Application.Features.Projects.DTOs.ProjectDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetProjects([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null, CancellationToken cancellationToken = default)
    {
        if (_currentUser.Id == null) return Unauthorized();

        var query = new GetProjectsQuery(_currentUser.Id.Value, page, pageSize, search);
        Result<GodForge.Application.Common.Models.PagedResult<GodForge.Application.Features.Projects.DTOs.ProjectDto>> result = await _mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Retrieves a paginated list of jobs for a specific project.
    /// </summary>
    [HttpGet("{projectId}/jobs")]
    [ProducesResponseType(typeof(ApiPagedResponse<GodForge.Application.Features.Jobs.DTOs.JobDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetJobs(Guid projectId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        if (_currentUser.Id == null) return Unauthorized();

        var query = new GetProjectJobsQuery(projectId, _currentUser.Id.Value, page, pageSize);
        Result<GodForge.Application.Common.Models.PagedResult<GodForge.Application.Features.Jobs.DTOs.JobDto>> result = await _mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Retrieves a paginated list of activities for a specific project.
    /// </summary>
    [HttpGet("{projectId}/activities")]
    [ProducesResponseType(typeof(ApiPagedResponse<GodForge.Application.Features.Activities.DTOs.ActivityDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetActivities(Guid projectId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        if (_currentUser.Id == null) return Unauthorized();

        var query = new GetProjectActivitiesQuery(projectId, _currentUser.Id.Value, page, pageSize);
        Result<GodForge.Application.Common.Models.PagedResult<GodForge.Application.Features.Activities.DTOs.ActivityDto>> result = await _mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }
}

public record CreateProjectRequest(string Name, string? Description, string Visibility);
