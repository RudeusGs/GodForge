namespace GodForge.Domain.Enums;

/// <summary>
/// Defines whether source code is hosted by GodForge's Git engine or linked from an external provider.
/// </summary>
public enum RepositoryMode
{
    ExternalLinked,
    InternalHosted
}
