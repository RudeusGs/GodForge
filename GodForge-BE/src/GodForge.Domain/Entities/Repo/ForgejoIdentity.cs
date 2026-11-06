using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Repo;

public sealed class ForgejoIdentity : BaseEntity
{
    public Guid UserId { get; private set; }
    public long ProviderUserId { get; private set; }
    public string Login { get; private set; } = "";
    public DateTimeOffset VerifiedAt { get; private set; }
    private ForgejoIdentity() { }
    public static ForgejoIdentity Bind(Guid userId, long providerUserId, string login, DateTimeOffset now)
    {
        if (userId == Guid.Empty || providerUserId <= 0) throw new ArgumentException("A user identity is required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(login);
        return new() { Id = Guid.NewGuid(), UserId = userId, ProviderUserId = providerUserId, Login = login, VerifiedAt = now };
    }
}
