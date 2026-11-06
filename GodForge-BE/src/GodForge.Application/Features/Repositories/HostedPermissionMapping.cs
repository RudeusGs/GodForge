using GodForge.Domain.Enums;

namespace GodForge.Application.Features.Repositories;

public static class HostedPermissionMapping
{
    public static string For(ProjectRole role) => role switch
    {
        ProjectRole.ProjectOwner => "admin",
        ProjectRole.Maintainer or ProjectRole.Developer => "write",
        ProjectRole.Reviewer or ProjectRole.Viewer => "read",
        _ => throw new ArgumentOutOfRangeException(nameof(role))
    };
}
