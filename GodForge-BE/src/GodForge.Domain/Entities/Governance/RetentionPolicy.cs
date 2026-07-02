using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Governance;

public sealed class RetentionPolicy : BaseAuditableEntity
{
    public string Scope { get; private set; } = default!;
    public string Target { get; private set; } = default!;
    public int RetentionDays { get; private set; }
    public string Action { get; private set; } = default!;
    public bool IsEnabled { get; private set; }

    private RetentionPolicy() { } // EF Core

    public static RetentionPolicy Create(
        string scope, string target, int retentionDays,
        string action, bool isEnabled)
    {
        return new RetentionPolicy
        {
            Id = Guid.NewGuid(),
            Scope = scope,
            Target = target,
            RetentionDays = retentionDays,
            Action = action,
            IsEnabled = isEnabled
        };
    }
}
