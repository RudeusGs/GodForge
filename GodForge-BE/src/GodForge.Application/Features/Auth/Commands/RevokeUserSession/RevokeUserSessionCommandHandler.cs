using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Application.Common.Models;
using MediatR;

namespace GodForge.Application.Features.Auth.Commands.RevokeUserSession;

public sealed class RevokeUserSessionCommandHandler : IRequestHandler<RevokeUserSessionCommand, Result>
{
    private readonly IUserSessionRepository _sessions;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IAuditWriter _auditWriter;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public RevokeUserSessionCommandHandler(
        IUserSessionRepository sessions,
        IRefreshTokenRepository refreshTokens,
        IAuditWriter auditWriter,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _sessions = sessions;
        _refreshTokens = refreshTokens;
        _auditWriter = auditWriter;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result> Handle(RevokeUserSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await _sessions.GetByIdAsync(request.SessionId, cancellationToken);
        if (session is null || session.UserId != request.UserId)
            return ApplicationError.NotFound("RESOURCE_NOT_FOUND", "The session was not found.");
        var now = _clock.UtcNow;
        session.Revoke("user-revoked", now);
        await _refreshTokens.RevokeAllForSessionAsync(session.Id, "user-revoked", now, cancellationToken);
        await _auditWriter.WriteSecurityAsync(request.UserId, "auth.session_revoked", "high", new { SessionId = session.Id }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
