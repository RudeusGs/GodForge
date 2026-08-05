using GodForge.Application.Common.Interfaces;
using GodForge.Application.Features.Organizations;
using GodForge.Application.Features.Organizations.DTOs;
using GodForge.Application.Features.Projects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;

namespace GodForge.Api.Controllers;

[Authorize]
public sealed class OrganizationsController : BaseApiController
{
    private readonly IMediator _mediator;
    private readonly IProjectManagementService _projects;
    private readonly ICurrentUser _currentUser;

    public OrganizationsController(IMediator mediator, IProjectManagementService projects, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _projects = projects;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? status = null, CancellationToken cancellationToken = default)
        => HandleResult(await _mediator.Send(new GodForge.Application.Features.Organizations.Queries.ListOrganizations.ListOrganizationsQuery(ActorId, page, pageSize, status), cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateOrganizationRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GodForge.Application.Features.Organizations.Commands.CreateOrganization.CreateOrganizationCommand(ActorId, request.Name, request.Slug, idempotencyKey), cancellationToken);
        if (result.IsError) return HandleResult(result);
        return CreatedAtAction(nameof(Get), new { organizationId = result.Value.Id }, Envelope(result.Value));
    }

    [HttpGet("{organizationId:guid}")]
    public async Task<IActionResult> Get(Guid organizationId, CancellationToken cancellationToken)
        => HandleResult(await _mediator.Send(new GodForge.Application.Features.Organizations.Queries.GetOrganization.GetOrganizationQuery(ActorId, organizationId), cancellationToken));

    [HttpPatch("{organizationId:guid}")]
    public async Task<IActionResult> Update(Guid organizationId, [FromBody] UpdateOrganizationRequest request, CancellationToken cancellationToken)
        => HandleResult(await _mediator.Send(new GodForge.Application.Features.Organizations.Commands.UpdateOrganization.UpdateOrganizationCommand(ActorId, organizationId, request.Name, request.Slug, request.Version), cancellationToken));

    [HttpDelete("{organizationId:guid}")]
    public async Task<IActionResult> Delete(Guid organizationId, [FromBody] DeleteOrganizationRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GodForge.Application.Features.Organizations.Commands.RequestOrganizationDeletion.RequestOrganizationDeletionCommand(ActorId, organizationId, request.Version, request.ConfirmationSlug), cancellationToken);
        if (result.IsError) return HandleResult(result);
        return Accepted(Envelope(result.Value));
    }

    [HttpPost("{organizationId:guid}/transfer-ownership")]
    public async Task<IActionResult> TransferOwnership(Guid organizationId, [FromBody] TransferOrganizationOwnershipRequest request, CancellationToken cancellationToken)
        => HandleResult(await _mediator.Send(new GodForge.Application.Features.Organizations.Commands.TransferOrganizationOwnership.TransferOrganizationOwnershipCommand(ActorId, organizationId, request.NewOwnerUserId, request.RetainCurrentOwnerAs, request.Version), cancellationToken));


    [HttpGet("{organizationId:guid}/projects")]
    public async Task<IActionResult> ListProjects(Guid organizationId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
        => HandleResult(await _projects.ListForOrganizationAsync(ActorId, organizationId, page, pageSize, cancellationToken));

    [HttpPost("{organizationId:guid}/projects")]
    public async Task<IActionResult> CreateProject(
        Guid organizationId,
        [FromBody] CreateOrganizationProjectRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await _projects.CreateAsync(ActorId, organizationId, request.Name, request.Slug, request.Description, request.Visibility, idempotencyKey, cancellationToken);
        if (result.IsError) return HandleResult(result);
        return CreatedAtAction("Get", "Projects", new { projectId = result.Value.Id }, Envelope(result.Value));
    }

    [HttpGet("{organizationId:guid}/members")]
    public async Task<IActionResult> ListMembers(Guid organizationId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? role = null, [FromQuery] string? status = null, [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
        => HandleResult(await _mediator.Send(new GodForge.Application.Features.Organizations.Queries.ListOrganizationMembers.ListOrganizationMembersQuery(ActorId, organizationId, page, pageSize, role, status, search), cancellationToken));

    [HttpPatch("{organizationId:guid}/members/{userId:guid}")]
    public async Task<IActionResult> UpdateMember(Guid organizationId, Guid userId, [FromBody] UpdateOrganizationMemberRequest request, CancellationToken cancellationToken)
        => HandleResult(await _mediator.Send(new GodForge.Application.Features.Organizations.Commands.UpdateOrganizationMember.UpdateOrganizationMemberCommand(ActorId, organizationId, userId, request.Role, request.Status, request.Version), cancellationToken));

    [HttpDelete("{organizationId:guid}/members/{userId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid organizationId, Guid userId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GodForge.Application.Features.Organizations.Commands.RemoveOrganizationMember.RemoveOrganizationMemberCommand(ActorId, organizationId, userId), cancellationToken);
        return result.IsSuccess ? NoContent() : HandleResult(result);
    }

    [HttpGet("{organizationId:guid}/invitations")]
    public async Task<IActionResult> ListInvitations(Guid organizationId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null, [FromQuery] string? email = null, CancellationToken cancellationToken = default)
        => HandleResult(await _mediator.Send(new GodForge.Application.Features.Organizations.Queries.ListOrganizationInvitations.ListOrganizationInvitationsQuery(ActorId, organizationId, page, pageSize, status, email), cancellationToken));

    [HttpPost("{organizationId:guid}/invitations")]
    public async Task<IActionResult> CreateInvitation(Guid organizationId, [FromBody] CreateOrganizationInvitationRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GodForge.Application.Features.Organizations.Commands.CreateOrganizationInvitation.CreateOrganizationInvitationCommand(ActorId, organizationId, request.Email, request.Role), cancellationToken);
        if (result.IsError) return HandleResult(result);
        return CreatedAtAction(nameof(ListInvitations), new { organizationId }, Envelope(result.Value));
    }

    [HttpDelete("{organizationId:guid}/invitations/{invitationId:guid}")]
    public async Task<IActionResult> RevokeInvitation(Guid organizationId, Guid invitationId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GodForge.Application.Features.Organizations.Commands.RevokeOrganizationInvitation.RevokeOrganizationInvitationCommand(ActorId, organizationId, invitationId), cancellationToken);
        return result.IsSuccess ? NoContent() : HandleResult(result);
    }

    private Guid ActorId => _currentUser.Id ?? throw new UnauthorizedAccessException("Authenticated user identifier is missing.");
    private ApiResponse<T> Envelope<T>(T value) => new() { Data = value, Meta = new ApiMeta { CorrelationId = CorrelationId } };
}

public sealed record CreateOrganizationRequest(string Name, string Slug);
public sealed record UpdateOrganizationRequest(string? Name, string? Slug, long Version);
public sealed record DeleteOrganizationRequest(long Version, string ConfirmationSlug);
public sealed record TransferOrganizationOwnershipRequest(Guid NewOwnerUserId, string RetainCurrentOwnerAs, long Version);
public sealed record UpdateOrganizationMemberRequest(string Role, string Status, long Version);
public sealed record CreateOrganizationInvitationRequest(string Email, string Role);

public sealed record CreateOrganizationProjectRequest(string Name, string Slug, string? Description, string Visibility);
