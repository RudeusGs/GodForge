namespace GodForge.Application.Common.Security;

public static class Permissions
{
    public const string ProjectsRead = "projects.read";
    public const string ProjectsCreate = "projects.create";
    public const string ProjectsUpdate = "projects.update";
    public const string ProjectsArchive = "projects.archive";
    public const string ProjectsRestore = "projects.restore";
    public const string ProjectsDelete = "projects.delete";
    public const string ProjectMembersRead = "projectMembers.read";
    public const string ProjectMembersAdd = "projectMembers.add";
    public const string ProjectMembersUpdateRole = "projectMembers.updateRole";
    public const string ProjectMembersRemove = "projectMembers.remove";
    public const string ProjectMembersTransferOwnership = "projectMembers.transferOwnership";
    public const string RepositoryRead = "repositories.read";
    public const string RepositoryManage = "repositories.manage";
    public const string RepositoryPush = "repositories.push";
    public const string RepositorySync = "repositories.sync";
    public const string RevisionsRead = "revisions.read";
    public const string AnalysisTrigger = "analysis.trigger";
    public const string AnalysisRead = "analysis.read";
    public const string AnalysisManage = "analysis.manage";
    public const string JobsRead = "jobs.read";
    public const string JobsCancel = "jobs.cancel";
    public const string ActivityRead = "activity.read";
    public const string NotificationsRead = "notifications.read";
    public const string SettingsUpdate = "settings.update";

    public const string OrganizationsRead = "organizations.read";
    public const string OrganizationsUpdate = "organizations.update";
    public const string OrganizationsDelete = "organizations.delete";
    public const string OrganizationsTransferOwnership = "organizations.transferOwnership";
    public const string OrganizationMembersRead = "organizationMembers.read";
    public const string OrganizationMembersInvite = "organizationMembers.invite";
    public const string OrganizationMembersUpdateRole = "organizationMembers.updateRole";
    public const string OrganizationMembersRemove = "organizationMembers.remove";
    public const string OrganizationProjectsListMetadata = "organizationProjects.listMetadata";
    public const string OrganizationProjectsCreate = "organizationProjects.create";

    // Compatibility aliases for existing handlers during migration.
    public const string RepositoryConfigure = RepositoryManage;
    public const string GitRead = RevisionsRead;
    public const string GitWrite = RepositoryPush;
    public const string MetadataRead = AnalysisRead;
    public const string JobsCreate = AnalysisTrigger;
    public const string ProjectsMembersManage = ProjectMembersAdd;
}
