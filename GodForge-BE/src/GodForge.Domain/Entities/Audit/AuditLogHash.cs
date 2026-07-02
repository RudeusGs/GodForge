using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Audit;

public sealed class AuditLogHash : BaseEntity
{
    public Guid AuditLogId { get; private set; }
    public string? PreviousHash { get; private set; }
    public string CurrentHash { get; private set; } = default!;
    public string Algorithm { get; private set; } = default!;
    public DateTimeOffset CreatedAt { get; private set; }

    private AuditLogHash() { } // EF Core

    public static AuditLogHash Create(
        Guid auditLogId, string? previousHash, string currentHash,
        string algorithm, DateTimeOffset now)
    {
        return new AuditLogHash
        {
            Id = Guid.NewGuid(),
            AuditLogId = auditLogId,
            PreviousHash = previousHash,
            CurrentHash = currentHash,
            Algorithm = algorithm,
            CreatedAt = now
        };
    }
}
