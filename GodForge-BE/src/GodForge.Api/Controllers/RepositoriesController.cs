using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Models;
using GodForge.Application.Features.Repositories.Commands.LinkRepository;
using GodForge.Application.Features.Repositories.Commands.TriggerRepositoryAnalysis;
using GodForge.Application.Features.Repositories.DTOs;
using GodForge.Application.Features.Repositories.Queries.GetRepository;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GodForge.Api.Controllers;

[Authorize]
public sealed class RepositoriesController : BaseApiController
{
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;

    public RepositoriesController(IMediator mediator, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpPost("/api/v1/projects/{projectId:guid}/repository/link")]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> LinkRepository(
        Guid projectId,
        [FromBody] LinkRepositoryRequest request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.Id is null)
        {
            return Unauthorized();
        }

        Result<RepositoryDto> result = await _mediator.Send(new LinkRepositoryCommand(
            projectId,
            request.RemoteUrl,
            request.Provider,
            request.DefaultBranch,
            request.ExternalRepositoryId,
            request.AutoAnalyzeEnabled,
            _currentUser.Id.Value,
            CorrelationId), cancellationToken);

        if (!result.IsSuccess)
        {
            return HandleResult(result);
        }

        return StatusCode(StatusCodes.Status201Created, new
        {
            data = result.Value,
            meta = new { correlationId = CorrelationId }
        });
    }

    [HttpGet("/api/v1/projects/{projectId:guid}/repository")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRepository(Guid projectId, CancellationToken cancellationToken)
    {
        if (_currentUser.Id is null)
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(
            new GetRepositoryQuery(projectId, _currentUser.Id.Value),
            cancellationToken);
        return HandleResult(result);
    }

    [HttpPost("/api/v1/projects/{projectId:guid}/repository/analyze")]
    [ProducesResponseType(typeof(object), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AnalyzeRepository(
        Guid projectId,
        [FromBody] TriggerRepositoryAnalysisRequest request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.Id is null)
        {
            return Unauthorized();
        }

        Result<Guid> result = await _mediator.Send(new TriggerRepositoryAnalysisCommand(
            projectId,
            request.Branch,
            request.AnalysisProfile,
            request.IncludeAi,
            _currentUser.Id.Value,
            CorrelationId), cancellationToken);

        if (!result.IsSuccess)
        {
            return HandleResult(result);
        }

        return Accepted(new
        {
            data = new { jobId = result.Value },
            meta = new { correlationId = CorrelationId }
        });
    }
}

public sealed record LinkRepositoryRequest(
    string RemoteUrl,
    string Provider,
    string DefaultBranch,
    string? ExternalRepositoryId,
    bool AutoAnalyzeEnabled);

public sealed record TriggerRepositoryAnalysisRequest(
    string? Branch,
    string AnalysisProfile = "health_overview",
    bool IncludeAi = false);
