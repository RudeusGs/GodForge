using GodForge.Application.Common.Models;
using GodForge.Application.Features.Auth.Commands.ForgotPassword;
using GodForge.Application.Features.Auth.Commands.Login;
using GodForge.Application.Features.Auth.Commands.Logout;
using GodForge.Application.Features.Auth.Commands.Register;
using GodForge.Application.Features.Auth.Commands.SendRegisterOtp;
using GodForge.Application.Features.Auth.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GodForge.Api.Controllers;

public class AuthController : BaseApiController
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Authenticates a user and returns a JWT token.
    /// </summary>
    [HttpPost("login")]
    [EnableRateLimiting("auth-sensitive")]
    [ProducesResponseType(typeof(ApiResponse<AuthResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var command = new LoginCommand(request.Email, request.Password);
        Result<AuthResultDto> result = await _mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Sends a registration OTP to the provided email address.
    /// </summary>
    [HttpPost("register/send-otp")]
    [EnableRateLimiting("auth-otp")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SendRegisterOtp([FromBody] SendRegisterOtpRequest request, CancellationToken cancellationToken)
    {
        var command = new SendRegisterOtpCommand(request.Email);
        Result result = await _mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Registers a new user account using an OTP.
    /// </summary>
    [HttpPost("register")]
    [EnableRateLimiting("auth-sensitive")]
    [ProducesResponseType(typeof(ApiResponse<AuthResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var command = new RegisterCommand(request.Email, request.DisplayName, request.Password, request.Otp);
        Result<AuthResultDto> result = await _mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Initiates the password reset process by sending an OTP to the user's email.
    /// </summary>
    [HttpPost("forgot-password")]
    [EnableRateLimiting("auth-otp")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        var command = new ForgotPasswordCommand(request.Email);
        Result result = await _mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Logs out the current user and invalidates the refresh token.
    /// </summary>
    [HttpPost("logout")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request, CancellationToken cancellationToken)
    {
        var command = new LogoutCommand(request.RefreshToken);
        Result result = await _mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Resets the user's password using the provided token.
    /// </summary>
    [HttpPost("reset-password")]
    [EnableRateLimiting("auth-sensitive")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var command = new GodForge.Application.Features.Auth.Commands.ResetPassword.ResetPasswordCommand(request.Email, request.Token, request.NewPassword);
        Result result = await _mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Refreshes the JWT token using a valid refresh token.
    /// </summary>
    [HttpPost("refresh")]
    [EnableRateLimiting("auth-sensitive")]
    [ProducesResponseType(typeof(ApiResponse<AuthResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var command = new GodForge.Application.Features.Auth.Commands.RefreshToken.RefreshTokenCommand(request.RefreshToken);
        Result<AuthResultDto> result = await _mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }
}

public record LoginRequest(string Email, string Password);
public record RegisterRequest(string Email, string DisplayName, string Password, string Otp);
public record SendRegisterOtpRequest(string Email);
public record LogoutRequest(string? RefreshToken);
public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Email, string Token, string NewPassword);
public record RefreshTokenRequest(string RefreshToken);


