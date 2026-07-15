using GodForge.Domain.Enums;

namespace GodForge.Application.Common.Security;

public static class RolePermissions
{
    public static IReadOnlySet<string> GetPermissionsForRole(ProjectRole role)
    {
        var commonRead = new[]
        {
            Permissions.ProjectsRead,
            Permissions.RepositoryRead,
            Permissions.RevisionsRead,
            Permissions.AnalysisRead,
            Permissions.JobsRead,
            Permissions.ActivityRead
        };

        return role switch
        {
            ProjectRole.Viewer => new HashSet<string>(commonRead),
            ProjectRole.Reviewer => new HashSet<string>(commonRead),
            ProjectRole.Developer => new HashSet<string>(commonRead)
            {
                Permissions.RepositoryPush,
                Permissions.RepositorySync,
                Permissions.AnalysisTrigger,
                Permissions.JobsCancel
            },
            ProjectRole.ProjectAdmin => new HashSet<string>(commonRead)
            {
                Permissions.ProjectsUpdate,
                Permissions.ProjectsMembersManage,
                Permissions.RepositoryManage,
                Permissions.RepositoryPush,
                Permissions.RepositorySync,
                Permissions.AnalysisTrigger,
                Permissions.AnalysisManage,
                Permissions.JobsCancel,
                Permissions.SettingsUpdate
            },
            ProjectRole.ProjectOwner => new HashSet<string>(commonRead)
            {
                Permissions.ProjectsUpdate,
                Permissions.ProjectsDelete,
                Permissions.ProjectsMembersManage,
                Permissions.RepositoryManage,
                Permissions.RepositoryPush,
                Permissions.RepositorySync,
                Permissions.AnalysisTrigger,
                Permissions.AnalysisManage,
                Permissions.JobsCancel,
                Permissions.SettingsUpdate
            },
            _ => new HashSet<string>()
        };
    }
}
