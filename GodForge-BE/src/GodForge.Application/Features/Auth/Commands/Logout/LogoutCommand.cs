using GodForge.Application.Common.Models;
using MediatR;

namespace GodForge.Application.Features.Auth.Commands.Logout;

public sealed record LogoutCommand : IRequest<Result>;
