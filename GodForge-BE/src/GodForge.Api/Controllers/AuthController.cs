using GodForge.Application.Common.Models;
using GodForge.Application.Features.Auth.Commands.Login;
using GodForge.Application.Features.Auth.Commands.Register;
using GodForge.Application.Features.Auth.Commands.SendRegisterOtp;
using GodForge.Application.Features.Auth.DTOs;
using GodForge.Application.Features.Auth.Commands.Logout;
using GodForge.Application.Features.Auth.Commands.ForgotPassword;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GodForge.Api.Controllers;

public class AuthController : BaseApiController
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var command = new LoginCommand(request.Email, request.Password);
        Result<AuthResultDto> result = await _mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    [HttpPost("register/send-otp")]
    public async Task<IActionResult> SendRegisterOtp([FromBody] SendRegisterOtpRequest request, CancellationToken cancellationToken)
    {
        var command = new SendRegisterOtpCommand(request.Email);
        Result result = await _mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var command = new RegisterCommand(request.Email, request.DisplayName, request.Password, request.Otp);
        Result<AuthResultDto> result = await _mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        var command = new ForgotPasswordCommand(request.Email);
        Result result = await _mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    [HttpPost("logout")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request, CancellationToken cancellationToken)
    {
        var command = new LogoutCommand(request.RefreshToken);
        Result result = await _mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }
}

public record LoginRequest(string Email, string Password);
public record RegisterRequest(string Email, string DisplayName, string Password, string Otp);
public record SendRegisterOtpRequest(string Email);
public record LogoutRequest(string? RefreshToken);
public record ForgotPasswordRequest(string Email);
