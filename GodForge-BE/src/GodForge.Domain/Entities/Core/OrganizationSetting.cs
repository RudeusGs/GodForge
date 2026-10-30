using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Core;

public sealed class OrganizationSetting : BaseAuditableEntity
{
    public Guid OrganizationId { get; private set; }
    public bool AiAdvisoryAllowed { get; private set; } = true;
    public long Version { get; private set; } = 1;
    private OrganizationSetting() { }
    public static OrganizationSetting Create(Guid organizationId, DateTimeOffset now) => new()
    {
        Id = Guid.NewGuid(),
        OrganizationId = organizationId,
        CreatedAt = now,
        UpdatedAt = now
    };
    public void Update(bool aiAdvisoryAllowed, long expectedVersion, DateTimeOffset now)
    {
        if (Version != expectedVersion) throw new InvalidOperationException("Concurrency version mismatch.");
        AiAdvisoryAllowed = aiAdvisoryAllowed;
        Version++;
        UpdatedAt = now;
    }
}
