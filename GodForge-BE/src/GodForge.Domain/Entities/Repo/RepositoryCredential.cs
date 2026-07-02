using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Repo;

public sealed class RepositoryCredential : BaseAuditableEntity
{
    public Guid RepositoryId { get; private set; }
    public string CredentialType { get; private set; } = default!;
    public bool IsActive { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }

    private RepositoryCredential() { } // EF Core

    public static RepositoryCredential Create(Guid repositoryId, string credentialType, DateTimeOffset? expiresAt, DateTimeOffset now)
    {
        return new RepositoryCredential
        {
            Id = Guid.NewGuid(),
            RepositoryId = repositoryId,
            CredentialType = credentialType,
            IsActive = true,
            ExpiresAt = expiresAt,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Deactivate(DateTimeOffset now)
    {
        if (IsActive)
        {
            IsActive = false;
            UpdatedAt = now;
        }
    }
}
