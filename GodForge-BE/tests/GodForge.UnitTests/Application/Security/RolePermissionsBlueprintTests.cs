using GodForge.Application.Common.Security;
using GodForge.Domain.Enums;

namespace GodForge.UnitTests.Application.Security;

public sealed class RolePermissionsBlueprintTests
{
    [Fact]
    public void Viewer_CanReadButCannotTriggerAnalysis()
    {
        var permissions = RolePermissions.GetPermissionsForRole(ProjectRole.Viewer);

        Assert.Contains(Permissions.RepositoryRead, permissions);
        Assert.Contains(Permissions.AnalysisRead, permissions);
        Assert.DoesNotContain(Permissions.AnalysisTrigger, permissions);
    }

    [Fact]
    public void Developer_CanSyncAndTriggerAnalysisButCannotManageRepository()
    {
        var permissions = RolePermissions.GetPermissionsForRole(ProjectRole.Developer);

        Assert.Contains(Permissions.RepositorySync, permissions);
        Assert.Contains(Permissions.AnalysisTrigger, permissions);
        Assert.DoesNotContain(Permissions.RepositoryManage, permissions);
    }
}
