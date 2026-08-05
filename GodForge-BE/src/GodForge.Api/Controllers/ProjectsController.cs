using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Models;
using GodForge.Application.Features.Activities.Queries.GetProjectActivities;
using GodForge.Application.Features.Jobs.Queries.GetProjectJobs;
using GodForge.Application.Features.Projects;
using GodForge.Application.Features.Projects.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GodForge.Api.Controllers;

[Authorize]
public sealed class ProjectsController : BaseApiController
{
    private readonly IMediator _mediator;
    private readonly IProjectManagementService _projects;
    private readonly ICurrentUser _currentUser;

    public ProjectsController(IMediator mediator, IProjectManagementService projects, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _projects = projects;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> GetProjects([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] Guid? organizationId = null, [FromQuery] string? status = null, [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
        => HandleResult(await _projects.ListAsync(ActorId, page, pageSize, organizationId, status, search, cancellationToken));

    [HttpGet("{projectId:guid}")]
    public async Task<IActionResult> Get(Guid projectId, CancellationToken cancellationToken)
        => HandleResult(await _projects.GetAsync(ActorId, projectId, cancellationToken));

    [HttpPatch("{projectId:guid}")]
    public async Task<IActionResult> Update(Guid projectId, [FromBody] UpdateProjectRequest request, CancellationToken cancellationToken)
        => HandleResult(await _projects.UpdateAsync(ActorId, projectId, request.Name, request.Slug, request.Description, request.Visibility, request.Version, cancellationToken));

    [HttpDelete("{projectId:guid}")]
    public async Task<IActionResult> Delete(Guid projectId, [FromBody] DeleteProjectRequest request, CancellationToken cancellationToken)
    {
        var result = await _projects.RequestDeletionAsync(ActorId, projectId, request.Version, request.ConfirmationSlug, cancellationToken);
        if (result.IsError) return HandleResult(result);
        return Accepted(Envelope(result.Value));
    }

    [HttpPost("{projectId:guid}/restore")]
    public async Task<IActionResult> Restore(Guid projectId, [FromBody] RestoreProjectRequest request, CancellationToken cancellationToken)
        => HandleResult(await _projects.RestoreAsync(ActorId, projectId, request.Version, cancellationToken));

    [HttpPost("{projectId:guid}/transfer-ownership")]
    public async Task<IActionResult> TransferOwnership(Guid projectId, [FromBody] TransferProjectOwnershipRequest request, CancellationToken cancellationToken)
        => HandleResult(await _projects.TransferOwnershipAsync(ActorId, projectId, request.NewOwnerUserId, request.RetainCurrentOwnerAs, request.Version, cancellationToken));

    [HttpGet("{projectId:guid}/members")]
    public async Task<IActionResult> ListMembers(Guid projectId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? role = null, [FromQuery] string? status = null, [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
        => HandleResult(await _projects.ListMembersAsync(ActorId, projectId, page, pageSize, role, status, search, cancellationToken));

    [HttpPost("{projectId:guid}/members")]
    public async Task<IActionResult> AddMember(Guid projectId, [FromBody] AddProjectMemberRequest request, CancellationToken cancellationToken)
    {
        var result = await _projects.AddMemberAsync(ActorId, projectId, request.UserId, request.Role, cancellationToken);
        if (result.IsError) return HandleResult(result);
        return CreatedAtAction(nameof(ListMembers), new { projectId }, Envelope(result.Value));
    }

    [HttpPatch("{projectId:guid}/members/{userId:guid}")]
    public async Task<IActionResult> UpdateMember(Guid projectId, Guid userId, [FromBody] UpdateProjectMemberRequest request, CancellationToken cancellationToken)
        => HandleResult(await _projects.UpdateMemberAsync(ActorId, projectId, userId, request.Role, request.Version, cancellationToken));

    [HttpDelete("{projectId:guid}/members/{userId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid projectId, Guid userId, CancellationToken cancellationToken)
    {
        var result = await _projects.RemoveMemberAsync(ActorId, projectId, userId, cancellationToken);
        return result.IsSuccess ? NoContent() : HandleResult(result);
    }

    [HttpGet("{projectId:guid}/settings")]
    public async Task<IActionResult> GetSettings(Guid projectId, CancellationToken cancellationToken)
        => HandleResult(await _projects.GetSettingsAsync(ActorId, projectId, cancellationToken));

    [HttpPut("{projectId:guid}/settings")]
    public async Task<IActionResult> UpdateSettings(Guid projectId, [FromBody] UpdateProjectSettingsRequest request, CancellationToken cancellationToken)
        => HandleResult(await _projects.UpdateSettingsAsync(ActorId, projectId, request.AnalysisProfileKey, request.AiAdvisoryEnabled,
            request.DefaultAssetVisibility, request.NotificationPolicyVersion, request.Version, cancellationToken));

    [HttpGet("{projectId:guid}/jobs")]
    public async Task<IActionResult> GetJobs(Guid projectId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var query = new GetProjectJobsQuery(projectId, ActorId, page, pageSize);
        Result<PagedResult<GodForge.Application.Features.Jobs.DTOs.JobDto>> result = await _mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    [HttpGet("{projectId:guid}/activities")]
    public async Task<IActionResult> GetActivities(Guid projectId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var query = new GetProjectActivitiesQuery(projectId, ActorId, page, pageSize);
        Result<PagedResult<GodForge.Application.Features.Activities.DTOs.ActivityDto>> result = await _mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    private Guid ActorId => _currentUser.Id ?? throw new UnauthorizedAccessException("Authenticated user identifier is missing.");
    private ApiResponse<T> Envelope<T>(T value) => new() { Data = value, Meta = new ApiMeta { CorrelationId = CorrelationId } };
}

public sealed record UpdateProjectRequest(string Name, string Slug, string? Description, string Visibility, long Version);
public sealed record DeleteProjectRequest(long Version, string ConfirmationSlug);
public sealed record RestoreProjectRequest(long Version);
public sealed record TransferProjectOwnershipRequest(Guid NewOwnerUserId, string RetainCurrentOwnerAs, long Version);
public sealed record AddProjectMemberRequest(Guid UserId, string Role);
public sealed record UpdateProjectMemberRequest(string Role, long Version);
public sealed record UpdateProjectSettingsRequest(string AnalysisProfileKey, bool AiAdvisoryEnabled, string DefaultAssetVisibility, int NotificationPolicyVersion, long Version);
