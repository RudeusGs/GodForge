namespace GodForge.Application.Common.Security;

public static class OrganizationSlugPolicy
{
    private static readonly HashSet<string> Reserved = new(StringComparer.OrdinalIgnoreCase)
    {
        "admin",
        "api",
        "app",
        "assets",
        "auth",
        "billing",
        "dashboard",
        "docs",
        "forgejo",
        "git",
        "help",
        "internal",
        "login",
        "logout",
        "new",
        "organizations",
        "projects",
        "root",
        "security",
        "settings",
        "signup",
        "status",
        "support",
        "system",
        "users",
        "www"
    };

    public static bool IsReserved(string slug) => Reserved.Contains(slug);
}
