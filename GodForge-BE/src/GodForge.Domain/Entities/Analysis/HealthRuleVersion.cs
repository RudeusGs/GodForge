using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Analysis;

public sealed class HealthRuleVersion : BaseEntity
{
    public Guid RuleId { get; private set; }
    public int Version { get; private set; }
    public string? ConfigJson { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private HealthRuleVersion() { } // EF Core

    public static HealthRuleVersion Create(
        Guid ruleId, int version, string? configJson, DateTimeOffset now)
    {
        return new HealthRuleVersion
        {
            Id = Guid.NewGuid(),
            RuleId = ruleId,
            Version = version,
            ConfigJson = configJson,
            CreatedAt = now
        };
    }
}
