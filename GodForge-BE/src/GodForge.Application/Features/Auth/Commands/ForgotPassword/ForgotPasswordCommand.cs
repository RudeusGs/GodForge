using GodForge.Application.Common.Models;
using MediatR;

namespace GodForge.Application.Features.Auth.Commands.ForgotPassword;

public record ForgotPasswordCommand(string Email) : IRequest<Result>;
