using GodForge.Domain.Common;

namespace GodForge.Domain.Entities.Analysis;

public sealed class HealthRule : BaseAuditableEntity
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public string DefaultSeverity { get; private set; } = default!;
    public bool IsEnabled { get; private set; }
    public string? ConfigJson { get; private set; }

    private HealthRule() { } // EF Core

    public static HealthRule Create(
        string code, string name, string? description,
        string defaultSeverity, bool isEnabled, string? configJson)
    {
        return new HealthRule
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            Description = description,
            DefaultSeverity = defaultSeverity,
            IsEnabled = isEnabled,
            ConfigJson = configJson
        };
    }
}
