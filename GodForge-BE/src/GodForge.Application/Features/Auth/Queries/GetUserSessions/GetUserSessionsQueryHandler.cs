using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models;
using GodForge.Application.Features.Auth.DTOs;
using MediatR;

namespace GodForge.Application.Features.Auth.Queries.GetUserSessions;

public sealed class GetUserSessionsQueryHandler : IRequestHandler<GetUserSessionsQuery, Result<IReadOnlyList<SessionDto>>>
{
    private const int MaxReturnedSessions = 100;
    private readonly IUserSessionRepository _sessions;
    private readonly IClock _clock;

    public GetUserSessionsQueryHandler(IUserSessionRepository sessions, IClock clock)
    {
        _sessions = sessions;
        _clock = clock;
    }

    public async Task<Result<IReadOnlyList<SessionDto>>> Handle(GetUserSessionsQuery request, CancellationToken cancellationToken)
    {
        var sessions = await _sessions.GetForUserAsync(
            request.UserId,
            request.CurrentSessionId,
            _clock.UtcNow,
            MaxReturnedSessions,
            cancellationToken);

        var result = sessions
            .OrderByDescending(x => x.Id == request.CurrentSessionId)
            .ThenByDescending(x => x.LastSeenAt ?? x.CreatedAt)
            .Select(x => SessionDto.From(x, request.CurrentSessionId))
            .ToList();
        return Result<IReadOnlyList<SessionDto>>.Success(result);
    }
}
