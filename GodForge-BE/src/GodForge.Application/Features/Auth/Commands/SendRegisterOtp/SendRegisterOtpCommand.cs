using GodForge.Application.Common.Models;
using GodForge.Application.Features.Auth.DTOs;
using MediatR;

namespace GodForge.Application.Features.Auth.Commands.SendRegisterOtp;

public sealed record SendRegisterOtpCommand(string Email, string CorrelationId) : IRequest<Result<ChallengeAcceptedDto>>;
