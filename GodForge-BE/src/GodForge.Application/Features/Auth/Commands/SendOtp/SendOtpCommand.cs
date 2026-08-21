using GodForge.Application.Common.Models;
using GodForge.Application.Features.Auth.DTOs;
using MediatR;

namespace GodForge.Application.Features.Auth.Commands.SendOtp;

public sealed record SendOtpCommand(string Email, string CorrelationId) : IRequest<Result<ChallengeAcceptedDto>>;
