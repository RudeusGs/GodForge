using GodForge.Api.Contracts;
using GodForge.Application.Common.Interfaces;
using GodForge.Application.Features.Auth.Commands.RevokeUserSession;
using GodForge.Application.Features.Auth.DTOs;
using GodForge.Application.Features.Auth.Queries.GetCurrentUser;
using GodForge.Application.Features.Auth.Queries.GetUserSessions;
using GodForge.Application.Features.Settings;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GodForge.Api.Controllers;

[Authorize]
public sealed class UsersController : BaseApiController
{
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;

    public UsersController(IMediator mediator, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpGet("me")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
        => HandleResult(await _mediator.Send(new GetCurrentUserQuery(_currentUser.GetId()), cancellationToken));

    [HttpGet("me/settings")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType(typeof(ApiResponse<UserPreferencesDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSettings(CancellationToken cancellationToken)
        => HandleResult(await _mediator.Send(new GetUserPreferencesQuery(_currentUser.GetId()), cancellationToken));

    [HttpPut("me/settings")]
    [ProducesResponseType(typeof(ApiResponse<UserPreferencesDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateSettings(UserPreferencesRequest request, CancellationToken cancellationToken)
        => HandleResult(await _mediator.Send(new UpdateUserPreferencesCommand(_currentUser.GetId(), request.Theme,
            request.NotificationInApp, request.NotificationEmail, request.NotificationDigest, request.Version), cancellationToken));

    [HttpGet("me/sessions")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<SessionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSessions(CancellationToken cancellationToken)
        => HandleResult(await _mediator.Send(new GetUserSessionsQuery(_currentUser.GetId(), _currentUser.GetSessionId()), cancellationToken));

    [HttpDelete("me/sessions/{sessionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RevokeSession(Guid sessionId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new RevokeUserSessionCommand(_currentUser.GetId(), sessionId), cancellationToken);
        return result.IsSuccess ? NoContent() : HandleResult(result);
    }
}
