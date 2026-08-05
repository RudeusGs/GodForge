using GodForge.Application.Common.Models;
using GodForge.Application.Features.Auth.DTOs;
using MediatR;

namespace GodForge.Application.Features.Auth.Commands.Login;

public sealed record LoginCommand(
    string Email,
    string Password,
    string? DeviceName,
    string? IpAddress,
    string? UserAgent) : IRequest<Result<AuthResultDto>>;
