using GodForge.Api.Contracts.Repositories;
using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Models;
using GodForge.Application.Features.Repositories.Commands.LinkRepository;
using GodForge.Application.Features.Repositories.Commands.ProvisionHostedRepository;
using GodForge.Application.Features.Repositories.Commands.SetBranchProtection;
using GodForge.Application.Features.Repositories.Commands.TriggerRepositoryAnalysis;
using GodForge.Application.Features.Repositories.DTOs;
using GodForge.Application.Features.Repositories.Queries.BrowseRepository;
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

    /// <summary>
    /// Links a remote Git repository to the project.
    /// </summary>
    [HttpPost("/api/v1/projects/{projectId:guid}/repository/link")]
    [ProducesResponseType(typeof(ApiResponse<RepositoryDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> LinkRepository(
        Guid projectId,
        [FromBody] LinkRepositoryRequest request,
        CancellationToken cancellationToken)
    {
        Result<RepositoryDto> result = await _mediator.Send(new LinkRepositoryCommand(
            projectId,
            request.RemoteUrl,
            request.Provider,
            request.DefaultBranch,
            request.ExternalRepositoryId,
            request.AutoAnalyzeEnabled,
            ActorId,
            CorrelationId), cancellationToken);

        if (!result.IsSuccess)
            return HandleResult(result);

        return CreatedAtAction(
            nameof(GetRepository),
            new { projectId },
            new ApiResponse<RepositoryDto>
            {
                Data = result.Value,
                Meta = new ApiMeta { CorrelationId = CorrelationId }
            });
    }

    /// <summary>
    /// Gets the linked repository for a project.
    /// </summary>
    [HttpGet("/api/v1/projects/{projectId:guid}/repository")]
    [ProducesResponseType(typeof(ApiResponse<RepositoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRepository(Guid projectId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetRepositoryQuery(projectId, ActorId), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Triggers an asynchronous analysis job for the repository.
    /// </summary>
    [HttpPost("/api/v1/projects/{projectId:guid}/repository/analyze")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AnalyzeRepository(
        Guid projectId,
        [FromBody] TriggerRepositoryAnalysisRequest request,
        CancellationToken cancellationToken)
    {
        Result<Guid> result = await _mediator.Send(new TriggerRepositoryAnalysisCommand(
            projectId,
            request.Branch,
            request.AnalysisProfile,
            request.IncludeAi,
            ActorId,
            CorrelationId), cancellationToken);

        if (!result.IsSuccess)
            return HandleResult(result);

        return Accepted(new ApiResponse<object>
        {
            Data = new { jobId = result.Value },
            Meta = new ApiMeta { CorrelationId = CorrelationId }
        });
    }

    private Guid ActorId => RequireActorId(_currentUser);

    [HttpGet("/api/v1/projects/{projectId:guid}/jobs/{jobId:guid}")]
    public async Task<IActionResult> GetOperation(Guid projectId, Guid jobId, CancellationToken cancellationToken)
        => HandleResult(await _mediator.Send(new GodForge.Application.Features.Jobs.Queries.GetJob.GetJobQuery(projectId, jobId, ActorId), cancellationToken));

    [HttpPost("/api/v1/projects/{projectId:guid}/repository/sync")]
    public async Task<IActionResult> Synchronize(Guid projectId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GodForge.Application.Features.Repositories.Commands.SynchronizeRepository.SynchronizeRepositoryCommand(projectId, ActorId, CorrelationId), cancellationToken);
        return result.IsSuccess ? Accepted(new ApiResponse<object> { Data = new { jobId = result.Value }, Meta = new ApiMeta { CorrelationId = CorrelationId } }) : HandleResult(result);
    }

    [HttpGet("/api/v1/projects/{projectId:guid}/repository/branch-protection")]
    public async Task<IActionResult> Protection(Guid projectId, string branch, CancellationToken cancellationToken)
        => HandleResult(await _mediator.Send(new BranchProtectionRequest(projectId, ActorId, branch, null), cancellationToken));

    public sealed record ProtectionBody(bool Enabled);
    [HttpPut("/api/v1/projects/{projectId:guid}/repository/branch-protection")]
    public async Task<IActionResult> Protection(Guid projectId, [FromQuery] string branch, [FromBody] ProtectionBody body, CancellationToken cancellationToken)
        => HandleResult(await _mediator.Send(new BranchProtectionRequest(projectId, ActorId, branch, body.Enabled), cancellationToken));

    [HttpGet("/api/v1/projects/{projectId:guid}/repository/branches")]
    public Task<IActionResult> Branches(Guid projectId, CancellationToken cancellationToken, int page = 1, int pageSize = 20)
        => Browse(new(projectId, ActorId, RepositoryBrowseOperation.Branches, Page: page, PageSize: pageSize), cancellationToken);

    [HttpGet("/api/v1/projects/{projectId:guid}/repository/commits")]
    public Task<IActionResult> Commits(Guid projectId, CancellationToken cancellationToken, string? branch = null, int page = 1, int pageSize = 20)
        => Browse(new(projectId, ActorId, RepositoryBrowseOperation.Commits, Branch: branch, Page: page, PageSize: pageSize), cancellationToken);

    [HttpGet("/api/v1/projects/{projectId:guid}/repository/tree")]
    public Task<IActionResult> Tree(Guid projectId, CancellationToken cancellationToken, string? commitSha = null, string path = "")
        => Browse(new(projectId, ActorId, RepositoryBrowseOperation.Tree, CommitSha: commitSha, Path: path), cancellationToken);

    [HttpGet("/api/v1/projects/{projectId:guid}/repository/blob")]
    public Task<IActionResult> Blob(Guid projectId, CancellationToken cancellationToken, string? commitSha = null, string path = "")
        => Browse(new(projectId, ActorId, RepositoryBrowseOperation.Blob, CommitSha: commitSha, Path: path), cancellationToken);

    private async Task<IActionResult> Browse(BrowseRepositoryQuery query, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "private, no-store";
        return HandleResult(await _mediator.Send(query, cancellationToken));
    }

    [HttpPost("/api/v1/projects/{projectId:guid}/repository/hosted")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status202Accepted)]
    public async Task<IActionResult> ProvisionHosted(Guid projectId, ProvisionHostedRepositoryRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ProvisionHostedRepositoryCommand(projectId, request.Name, request.DefaultBranch,
            request.IsPrivate, request.AutoAnalyzeEnabled, ActorId, CorrelationId), cancellationToken);
        return result.IsSuccess ? Accepted(new ApiResponse<object>
        {
            Data = new { jobId = result.Value },
            Meta = new ApiMeta { CorrelationId = CorrelationId }
        }) : HandleResult(result);
    }
}
