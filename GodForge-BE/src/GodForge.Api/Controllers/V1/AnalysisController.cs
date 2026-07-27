using GodForge.Application.Common.Interfaces;
using GodForge.Application.Features.Analysis.DTOs;
using GodForge.Application.Features.Analysis.Queries.GetAiAdvisory;
using GodForge.Application.Features.Analysis.Queries.GetDependencyGraph;
using GodForge.Application.Features.Analysis.Queries.GetHealthReport;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GodForge.Api.Controllers.V1;

[Authorize]
[Route("api/v1/projects/{projectId:guid}/[controller]")]
public sealed class AnalysisController : BaseApiController
{
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;

    public AnalysisController(IMediator mediator, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpGet("health/latest")]
    [ProducesResponseType(typeof(ApiResponse<HealthReportResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLatestHealthReport(Guid projectId, CancellationToken cancellationToken)
    {
        if (_currentUser.Id is null)
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(
            new GetHealthReportQuery(projectId, _currentUser.Id.Value),
            cancellationToken);
        return HandleResult(result);
    }

    [HttpGet("dependencies")]
    [ProducesResponseType(typeof(ApiResponse<DependencyGraphDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDependencyGraph(Guid projectId, CancellationToken cancellationToken)
    {
        if (_currentUser.Id is null)
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(
            new GetDependencyGraphQuery(projectId, _currentUser.Id.Value),
            cancellationToken);
        return HandleResult(result);
    }

    [HttpGet("ai-advisory/latest")]
    [ProducesResponseType(typeof(ApiResponse<AiAdvisoryResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLatestAiAdvisory(Guid projectId, CancellationToken cancellationToken)
    {
        if (_currentUser.Id is null)
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(
            new GetAiAdvisoryQuery(projectId, _currentUser.Id.Value),
            cancellationToken);
        return HandleResult(result);
    }
}
