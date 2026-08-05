using GodForge.Application.Common.Models;
using MediatR;

namespace GodForge.Application.Features.Auth.Commands.RevokeUserSession;

public sealed record RevokeUserSessionCommand(Guid UserId, Guid SessionId) : IRequest<Result>;
