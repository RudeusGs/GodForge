using GodForge.Api.Contracts.Auth;
using GodForge.Api.Models;
using GodForge.Api.Services;
using GodForge.Application.Common.Models;
using GodForge.Application.Features.Auth.Commands.ForgotPassword;
using GodForge.Application.Features.Auth.Commands.Login;
using GodForge.Application.Features.Auth.Commands.Logout;
using GodForge.Application.Features.Auth.Commands.RefreshToken;
using GodForge.Application.Features.Auth.Commands.Register;
using GodForge.Application.Features.Auth.Commands.ResetPassword;
using GodForge.Application.Features.Auth.Commands.SendRegisterOtp;
using GodForge.Application.Features.Auth.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GodForge.Api.Controllers;

public sealed class AuthController : BaseApiController
{
    private readonly IMediator _mediator;
    private readonly RefreshTokenCookieService _refreshTokenCookie;

    public AuthController(IMediator mediator, RefreshTokenCookieService refreshTokenCookie)
    {
        _mediator = mediator;
        _refreshTokenCookie = refreshTokenCookie;
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth-sensitive")]
    [ProducesResponseType(typeof(ApiResponse<AuthSessionResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var command = new LoginCommand(
            request.Email,
            request.Password,
            request.DeviceName,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString());
        var result = await _mediator.Send(command, cancellationToken);
        return HandleAuthResult(result);
    }

    [HttpPost("register/send-otp")]
    [EnableRateLimiting("auth-otp")]
    [ProducesResponseType(typeof(ApiResponse<ChallengeAcceptedDto>), StatusCodes.Status202Accepted)]
    public async Task<IActionResult> SendRegisterOtp([FromBody] SendRegisterOtpRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new SendRegisterOtpCommand(request.Email, CorrelationId), cancellationToken);
        if (!result.IsSuccess)
            return HandleResult(result);
        return Accepted(new ApiResponse<ChallengeAcceptedDto> { Data = result.Value, Meta = new ApiMeta { CorrelationId = CorrelationId } });
    }

    [HttpPost("register")]
    [EnableRateLimiting("auth-sensitive")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new RegisterCommand(request.Email, request.DisplayName, request.Password, request.Otp), cancellationToken);
        if (!result.IsSuccess)
            return HandleResult(result);
        return Created("/api/v1/users/me", new ApiResponse<UserDto> { Data = result.Value, Meta = new ApiMeta { CorrelationId = CorrelationId } });
    }

    [HttpPost("forgot-password")]
    [EnableRateLimiting("auth-otp")]
    [ProducesResponseType(typeof(ApiResponse<ChallengeAcceptedDto>), StatusCodes.Status202Accepted)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ForgotPasswordCommand(request.Email, CorrelationId), cancellationToken);
        if (!result.IsSuccess)
            return HandleResult(result);
        return Accepted(new ApiResponse<ChallengeAcceptedDto> { Data = result.Value, Meta = new ApiMeta { CorrelationId = CorrelationId } });
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new LogoutCommand(), cancellationToken);
        if (!result.IsSuccess)
            return HandleResult(result);

        _refreshTokenCookie.Delete(Response);
        return NoContent();
    }

    [HttpPost("reset-password")]
    [EnableRateLimiting("auth-sensitive")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ResetPasswordCommand(request.Email, request.Token, request.NewPassword), cancellationToken);
        return result.IsSuccess ? NoContent() : HandleResult(result);
    }

    [HttpPost("refresh")]
    [EnableRateLimiting("auth-sensitive")]
    [ProducesResponseType(typeof(ApiResponse<AuthSessionResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        var refreshToken = _refreshTokenCookie.Read(Request);
        if (refreshToken is null)
            return HandleResult<AuthResultDto>(ApplicationError.Unauthorized("AUTH_TOKEN_REVOKED", "Refresh token is invalid or revoked."));

        var result = await _mediator.Send(new RefreshTokenCommand(refreshToken), cancellationToken);
        if (!result.IsSuccess)
        {
            _refreshTokenCookie.Delete(Response);
            return HandleResult(result);
        }

        return HandleAuthResult(result);
    }

    private IActionResult HandleAuthResult(Result<AuthResultDto> result)
    {
        if (!result.IsSuccess)
            return HandleResult(result);

        _refreshTokenCookie.Write(Response, result.Value.RefreshToken, result.Value.RefreshTokenExpiresAt);
        return Ok(new ApiResponse<AuthSessionResponseDto>
        {
            Data = AuthSessionResponseDto.From(result.Value),
            Meta = new ApiMeta { CorrelationId = CorrelationId }
        });
    }
}
