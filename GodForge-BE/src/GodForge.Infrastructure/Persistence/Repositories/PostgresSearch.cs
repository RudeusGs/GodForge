namespace GodForge.Infrastructure.Persistence.Repositories;

internal static class PostgresSearch
{
    internal const string LikeEscapeCharacter = "\\";

    internal static string ContainsPattern(string value)
        => $"%{EscapeLikePattern(value.Trim())}%";

    private static string EscapeLikePattern(string value)
        => value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
}
