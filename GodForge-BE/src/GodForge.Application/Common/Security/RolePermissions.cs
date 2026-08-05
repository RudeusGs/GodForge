using GodForge.Domain.Enums;

namespace GodForge.Application.Common.Security;

public static class RolePermissions
{
    public static IReadOnlySet<string> GetPermissionsForRole(ProjectRole role)
    {
        var commonRead = new HashSet<string>(StringComparer.Ordinal)
        {
            Permissions.ProjectsRead,
            Permissions.ProjectMembersRead,
            Permissions.RepositoryRead,
            Permissions.RevisionsRead,
            Permissions.AnalysisRead,
            Permissions.JobsRead,
            Permissions.ActivityRead
        };

        switch (role)
        {
            case ProjectRole.Viewer:
            case ProjectRole.Reviewer:
                return commonRead;
            case ProjectRole.Developer:
                commonRead.UnionWith(new[] { Permissions.RepositoryPush, Permissions.RepositorySync, Permissions.AnalysisTrigger, Permissions.JobsCancel });
                return commonRead;
            case ProjectRole.Maintainer:
                commonRead.UnionWith(new[]
                {
                    Permissions.ProjectsUpdate, Permissions.ProjectsArchive, Permissions.ProjectsRestore,
                    Permissions.ProjectMembersAdd, Permissions.ProjectMembersUpdateRole, Permissions.ProjectMembersRemove,
                    Permissions.RepositoryManage, Permissions.RepositoryPush, Permissions.RepositorySync,
                    Permissions.AnalysisTrigger, Permissions.AnalysisManage, Permissions.JobsCancel, Permissions.SettingsUpdate
                });
                return commonRead;
            case ProjectRole.ProjectOwner:
                commonRead.UnionWith(new[]
                {
                    Permissions.ProjectsUpdate, Permissions.ProjectsArchive, Permissions.ProjectsRestore, Permissions.ProjectsDelete,
                    Permissions.ProjectMembersAdd, Permissions.ProjectMembersUpdateRole, Permissions.ProjectMembersRemove,
                    Permissions.ProjectMembersTransferOwnership, Permissions.RepositoryManage, Permissions.RepositoryPush,
                    Permissions.RepositorySync, Permissions.AnalysisTrigger, Permissions.AnalysisManage,
                    Permissions.JobsCancel, Permissions.SettingsUpdate
                });
                return commonRead;
            default:
                return new HashSet<string>(StringComparer.Ordinal);
        }
    }
}
