using GodForge.Api.Contracts.Organizations;
using GodForge.Application.Common.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GodForge.Api.Controllers;

[Authorize]
[Route("api/v1/organization-invitations")]
public sealed class OrganizationInvitationsController : BaseApiController
{
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;

    public OrganizationInvitationsController(IMediator mediator, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpPost("accept")]
    public async Task<IActionResult> Accept([FromBody] AcceptOrganizationInvitationRequest request, CancellationToken cancellationToken)
    {
        return HandleResult(await _mediator.Send(
            new GodForge.Application.Features.Organizations.Commands.AcceptOrganizationInvitation.AcceptOrganizationInvitationCommand(
                RequireActorId(_currentUser),
                request.Token),
            cancellationToken));
    }
}
