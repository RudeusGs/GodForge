namespace GodForge.Domain.Enums;

/// <summary>
/// Git providers supported by the repository adapter layer.
/// </summary>
public enum GitProvider
{
    Forgejo,
    GitHub,
    GitLab,
    Bitbucket,
    Generic
}
