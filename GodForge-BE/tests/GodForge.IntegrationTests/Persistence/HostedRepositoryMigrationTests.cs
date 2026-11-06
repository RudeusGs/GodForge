using GodForge.Domain.Entities.Core;
using GodForge.Domain.Entities.Identity;
using GodForge.Domain.Enums;
using GodForge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace GodForge.IntegrationTests.Persistence;

[Collection(PostgresPersistenceCollection.CollectionName)]
public sealed class HostedRepositoryMigrationTests(PostgresPersistenceFixture fixture)
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FreshAndPriorM1SchemaHaveConsistentHostedModelAndPreserveLinkedData(bool upgrade)
    {
        var connection = new NpgsqlConnectionStringBuilder(fixture.ConnectionString) { Database = $"m2_migration_{Guid.NewGuid():N}" };
        var options = new DbContextOptionsBuilder<GodForgeDbContext>().UseNpgsql(connection.ConnectionString).UseSnakeCaseNamingConvention().Options;
        await using var db = new GodForgeDbContext(options);
        var now = DateTimeOffset.UtcNow;
        var repositoryId = Guid.NewGuid();
        try
        {
            if (upgrade)
            {
                await db.GetService<IMigrator>().MigrateAsync("20261030055044_VersionedPreferencesAndOrganizationPolicy");
                var user = User.Create($"m2-{Guid.NewGuid():N}@example.test", "Preserved", "hash", now);
                var organization = Organization.Create("Preserved", $"preserved-{Guid.NewGuid():N}", user.Id, now);
                var project = Project.Create(organization.Id, "Preserved", "preserved", null, "4.4", ProjectVisibility.Private, user.Id, now);
                db.Users.Add(user); db.Organizations.Add(organization); db.Projects.Add(project); await db.SaveChangesAsync();
                await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO repo.repositories (id,project_id,mode,provider,remote_url,clone_url_sanitized,default_branch,status,auto_analyze_enabled,created_at,updated_at) VALUES ({repositoryId},{project.Id},{"ExternalLinked"},{"Generic"},{"https://example.test/game.git"},{"https://example.test/game.git"},{"main"},{"Configured"},{false},{now},{now})");
            }
            await db.Database.MigrateAsync();
            Assert.Empty(await db.Database.GetPendingMigrationsAsync());
            Assert.False(db.Database.HasPendingModelChanges());
            Assert.Empty(await db.Set<GodForge.Domain.Entities.Repo.ForgejoIdentity>().ToArrayAsync());
            if (upgrade)
            {
                var repository = await db.GitRepositories.SingleAsync(r => r.Id == repositoryId);
                Assert.Equal(RepositoryMode.ExternalLinked, repository.Mode);
                Assert.Equal("https://example.test/game.git", repository.CloneUrlSanitized);
                Assert.Null(repository.HostedName); Assert.Null(repository.ProvisionIdentity); Assert.Null(repository.AccessRequestedAt);
            }
        }
        finally
        {
            // Only the randomly named database created above belongs to this test.
            await db.Database.EnsureDeletedAsync();
        }
    }
}
