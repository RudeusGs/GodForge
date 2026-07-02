using GodForge.Domain.Enums;

namespace GodForge.Application.Common.Security;

public static class RolePermissions
{
    public static IReadOnlySet<string> GetPermissionsForRole(ProjectRole role)
    {
        return role switch
        {
            ProjectRole.Viewer => new HashSet<string> 
            { 
                Permissions.ProjectsRead, 
                Permissions.RepositoryRead,
                Permissions.GitRead,
                Permissions.MetadataRead,
                Permissions.JobsRead,
                Permissions.ActivityRead
            },
            ProjectRole.Reviewer => new HashSet<string>
            {
                Permissions.ProjectsRead,
                Permissions.RepositoryRead,
                Permissions.GitRead,
                Permissions.MetadataRead,
                Permissions.JobsRead,
                Permissions.ActivityRead
            },
            ProjectRole.Developer => new HashSet<string>
            {
                Permissions.ProjectsRead,
                Permissions.RepositoryRead,
                Permissions.GitRead,
                Permissions.GitWrite,
                Permissions.MetadataRead,
                Permissions.JobsRead,
                Permissions.JobsCreate,
                Permissions.ActivityRead
            },
            ProjectRole.ProjectAdmin => new HashSet<string>
            {
                Permissions.ProjectsRead,
                Permissions.ProjectsUpdate,
                Permissions.ProjectsMembersManage,
                Permissions.RepositoryConfigure,
                Permissions.RepositoryRead,
                Permissions.GitRead,
                Permissions.GitWrite,
                Permissions.MetadataRead,
                Permissions.JobsRead,
                Permissions.JobsCreate,
                Permissions.ActivityRead,
                Permissions.SettingsUpdate
            },
            ProjectRole.ProjectOwner => new HashSet<string>
            {
                Permissions.ProjectsRead,
                Permissions.ProjectsUpdate,
                Permissions.ProjectsDelete,
                Permissions.ProjectsMembersManage,
                Permissions.RepositoryConfigure,
                Permissions.RepositoryRead,
                Permissions.GitRead,
                Permissions.GitWrite,
                Permissions.MetadataRead,
                Permissions.JobsRead,
                Permissions.JobsCreate,
                Permissions.ActivityRead,
                Permissions.SettingsUpdate
            },
            _ => new HashSet<string>()
        };
    }
}
