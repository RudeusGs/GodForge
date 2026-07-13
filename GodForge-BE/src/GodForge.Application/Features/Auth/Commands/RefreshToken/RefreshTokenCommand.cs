using GodForge.Application.Common.Models;
using GodForge.Application.Features.Auth.DTOs;
using MediatR;

namespace GodForge.Application.Features.Auth.Commands.RefreshToken;

public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<Result<AuthResultDto>>;
