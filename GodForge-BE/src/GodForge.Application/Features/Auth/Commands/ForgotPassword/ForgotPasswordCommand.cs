using GodForge.Application.Common.Models;
using GodForge.Application.Features.Auth.DTOs;
using MediatR;

namespace GodForge.Application.Features.Auth.Commands.ForgotPassword;

public sealed record ForgotPasswordCommand(string Email, string CorrelationId) : IRequest<Result<ChallengeAcceptedDto>>;
