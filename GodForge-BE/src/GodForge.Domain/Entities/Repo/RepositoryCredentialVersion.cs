using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Repo;

public sealed class RepositoryCredentialVersion : BaseEntity
{
    public Guid CredentialId { get; private set; }
    public string EncryptedSecret { get; private set; } = default!;
    public string EncryptionKeyId { get; private set; } = default!;
    public Guid CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private RepositoryCredentialVersion() { } // EF Core

    public static RepositoryCredentialVersion Create(Guid credentialId, string encryptedSecret, string encryptionKeyId, Guid createdBy, DateTimeOffset now)
    {
        return new RepositoryCredentialVersion
        {
            Id = Guid.NewGuid(),
            CredentialId = credentialId,
            EncryptedSecret = encryptedSecret,
            EncryptionKeyId = encryptionKeyId,
            CreatedBy = createdBy,
            CreatedAt = now
        };
    }
}
