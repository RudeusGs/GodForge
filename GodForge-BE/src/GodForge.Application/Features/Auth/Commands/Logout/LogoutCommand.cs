using GodForge.Application.Common.Models;
using MediatR;

namespace GodForge.Application.Features.Auth.Commands.Logout;

public record LogoutCommand(string? RefreshToken) : IRequest<Result>;
