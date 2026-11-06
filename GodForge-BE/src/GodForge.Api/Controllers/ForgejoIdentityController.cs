using GodForge.Application.Common.Interfaces;
using GodForge.Application.Features.Repositories.Commands.BindForgejoIdentity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GodForge.Api.Controllers;

[Authorize]
public sealed class ForgejoIdentityController(IMediator mediator, ICurrentUser currentUser) : BaseApiController
{
    public sealed class BindRequest { public string PersonalToken { get; init; } = ""; public override string ToString() => nameof(BindRequest); }
    [HttpPost("/api/v1/users/me/forgejo-identity")]
    [RequestSizeLimit(2048)]
    public async Task<IActionResult> Bind(BindRequest request, CancellationToken cancellationToken)
        => HandleResult(await mediator.Send(new BindForgejoIdentityCommand(RequireActorId(currentUser), request.PersonalToken), cancellationToken));
}
