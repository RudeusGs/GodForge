using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models;
using GodForge.Application.Features.Auth.DTOs;
using MediatR;

namespace GodForge.Application.Features.Auth.Queries.GetUserSessions;

public sealed class GetUserSessionsQueryHandler : IRequestHandler<GetUserSessionsQuery, Result<IReadOnlyList<SessionDto>>>
{
    private readonly IUserSessionRepository _sessions;
    public GetUserSessionsQueryHandler(IUserSessionRepository sessions) => _sessions = sessions;

    public async Task<Result<IReadOnlyList<SessionDto>>> Handle(GetUserSessionsQuery request, CancellationToken cancellationToken)
    {
        var sessions = await _sessions.GetForUserAsync(request.UserId, cancellationToken);
        var result = sessions
            .OrderByDescending(x => x.Id == request.CurrentSessionId)
            .ThenByDescending(x => x.LastSeenAt ?? x.CreatedAt)
            .Select(x => SessionDto.From(x, request.CurrentSessionId))
            .ToList();
        return Result<IReadOnlyList<SessionDto>>.Success(result);
    }
}
