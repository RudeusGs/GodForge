using GodForge.Application.Features.Analysis.DTOs;
using GodForge.Application.Features.Analysis.Queries.GetDependencyGraph;
using GodForge.Application.Features.Analysis.Queries.GetHealthReport;
using GodForge.Application.Features.Analysis.Queries.GetAiAdvisory;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GodForge.Api.Controllers.V1;

[Route("api/v1/projects/{projectId:guid}/[controller]")]
public sealed class AnalysisController : BaseApiController
{
    private readonly IMediator _mediator;

    public AnalysisController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("health/latest")]
    [Authorize(Policy = "RequireProjectReader")]
    [ProducesResponseType(typeof(ApiResponse<HealthReportResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLatestHealthReport(Guid projectId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetHealthReportQuery(projectId), cancellationToken);
        return HandleResult(result);
    }

    [HttpGet("dependencies")]
    [Authorize(Policy = "RequireProjectReader")]
    [ProducesResponseType(typeof(ApiResponse<DependencyGraphDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDependencyGraph(Guid projectId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetDependencyGraphQuery(projectId), cancellationToken);
        return HandleResult(result);
    }

    [HttpGet("ai-advisory/latest")]
    [Authorize(Policy = "RequireProjectReader")]
    [ProducesResponseType(typeof(ApiResponse<AiAdvisoryResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLatestAiAdvisory(Guid projectId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetAiAdvisoryQuery(projectId), cancellationToken);
        return HandleResult(result);
    }
}
