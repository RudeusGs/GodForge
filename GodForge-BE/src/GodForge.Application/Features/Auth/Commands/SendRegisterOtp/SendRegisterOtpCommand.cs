using GodForge.Application.Common.Models;
using MediatR;

namespace GodForge.Application.Features.Auth.Commands.SendRegisterOtp;

public sealed record SendRegisterOtpCommand(string Email) : IRequest<Result>;
