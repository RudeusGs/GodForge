using GodForge.Application.Common.Models;
using GodForge.Application.Features.Auth.DTOs;
using MediatR;

namespace GodForge.Application.Features.Auth.Queries.GetUserSessions;

public sealed record GetUserSessionsQuery(Guid UserId, Guid CurrentSessionId) : IRequest<Result<IReadOnlyList<SessionDto>>>;
