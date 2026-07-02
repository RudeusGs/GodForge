using GodForge.Application.Common.Interfaces;
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

    [HttpPost]
    public async Task<IActionResult> CreateProject([FromBody] CreateProjectRequest request, CancellationToken cancellationToken)
    {
        if (_currentUser.Id == null) return Unauthorized();

        var command = new CreateProjectCommand(
            request.Name,
            request.Description,
            request.Visibility,
            _currentUser.Id.Value,
            CorrelationId);

        var result = await _mediator.Send(command, cancellationToken);
        if (result.IsSuccess)
        {
            return StatusCode(201, new 
            { 
                data = result.Value, 
                meta = new { correlationId = CorrelationId } 
            });
        }
        return HandleResult(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetProjects([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null, CancellationToken cancellationToken = default)
    {
        if (_currentUser.Id == null) return Unauthorized();

        var query = new GetProjectsQuery(_currentUser.Id.Value, page, pageSize, search);
        var result = await _mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    [HttpGet("{projectId}/jobs")]
    public async Task<IActionResult> GetJobs(Guid projectId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        if (_currentUser.Id == null) return Unauthorized();

        var query = new GetProjectJobsQuery(projectId, _currentUser.Id.Value, page, pageSize);
        var result = await _mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    [HttpGet("{projectId}/activities")]
    public async Task<IActionResult> GetActivities(Guid projectId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        if (_currentUser.Id == null) return Unauthorized();

        var query = new GetProjectActivitiesQuery(projectId, _currentUser.Id.Value, page, pageSize);
        var result = await _mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }
}

public record CreateProjectRequest(string Name, string? Description, string Visibility);
