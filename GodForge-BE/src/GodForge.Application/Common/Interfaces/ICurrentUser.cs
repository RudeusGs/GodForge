namespace GodForge.Application.Common.Interfaces;

public interface ICurrentUser
{
    Guid? Id { get; }
    Guid? SessionId { get; }
    bool IsAuthenticated { get; }
    string? Email { get; }
    string? SystemRole { get; }
    string? Jti { get; }
    DateTimeOffset? TokenExpiration { get; }

    Guid GetId();
    Guid GetSessionId();
}
