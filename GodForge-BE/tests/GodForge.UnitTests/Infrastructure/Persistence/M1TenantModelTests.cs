using GodForge.Domain.Entities.Core;
using GodForge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GodForge.UnitTests.Infrastructure.Persistence;

public sealed class M1TenantModelTests
{
    private static GodForgeDbContext CreateContext()
        => new(new DbContextOptionsBuilder<GodForgeDbContext>()
            .UseNpgsql("Host=localhost;Database=model_only;Username=model_only;Password=model_only")
            .UseSnakeCaseNamingConvention()
            .Options);

    [Fact]
    public void ProjectSlug_IsUniqueWithinOrganization_NotGlobally()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(Project))!;
        var index = Assert.Single(entity.GetIndexes(), index =>
            index.IsUnique && index.Properties.Select(property => property.Name)
                .SequenceEqual(new[] { nameof(Project.OrganizationId), nameof(Project.Slug) }));

        Assert.Equal("ux_projects_org_slug_active", index.GetDatabaseName());
    }

    [Fact]
    public void ProjectMember_HasCompositeTenantForeignKeys()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(ProjectMember))!;
        var foreignKeys = entity.GetForeignKeys().ToList();

        Assert.Contains(foreignKeys, foreignKey =>
            foreignKey.PrincipalEntityType.ClrType == typeof(Project) &&
            foreignKey.Properties.Select(property => property.Name)
                .SequenceEqual(new[] { nameof(ProjectMember.ProjectId), nameof(ProjectMember.OrganizationId) }));
        Assert.Contains(foreignKeys, foreignKey =>
            foreignKey.PrincipalEntityType.ClrType == typeof(OrganizationMember) &&
            foreignKey.Properties.Select(property => property.Name)
                .SequenceEqual(new[] { nameof(ProjectMember.OrganizationId), nameof(ProjectMember.UserId) }));
    }

    [Fact]
    public void ProjectSettings_AreTypedVersionedAndUniquePerProject()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(ProjectSetting))!;

        Assert.NotNull(entity.FindProperty(nameof(ProjectSetting.AnalysisProfileKey)));
        Assert.NotNull(entity.FindProperty(nameof(ProjectSetting.AiAdvisoryEnabled)));
        Assert.NotNull(entity.FindProperty(nameof(ProjectSetting.DefaultAssetVisibility)));
        Assert.True(entity.FindProperty(nameof(ProjectSetting.Version))!.IsConcurrencyToken);
        Assert.Contains(entity.GetIndexes(), index => index.IsUnique &&
            index.Properties.Select(property => property.Name).SequenceEqual(new[] { nameof(ProjectSetting.ProjectId) }));
    }

    [Fact]
    public void ProjectSlug_UsesDomainMaximumLength()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(Project))!;

        Assert.Equal(Project.MaxSlugLength, entity.FindProperty(nameof(Project.Slug))!.GetMaxLength());
    }
}
