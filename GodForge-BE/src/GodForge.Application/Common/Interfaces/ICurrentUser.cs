namespace GodForge.Application.Common.Interfaces;

public interface ICurrentUser
{
    Guid? Id { get; }
    bool IsAuthenticated { get; }
    string? Email { get; }
    string? SystemRole { get; }
    
    Guid GetId(); // Throws if not authenticated
}
