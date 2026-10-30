using GodForge.Domain.Entities.Core;
using GodForge.Domain.Entities.Identity;
using GodForge.Domain.Enums;
using GodForge.Infrastructure.Persistence;
using GodForge.IntegrationTests.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace GodForge.IntegrationTests.Settings;

[Collection(PostgresPersistenceCollection.CollectionName)]
public sealed class SettingsMigrationTests(PostgresPersistenceFixture fixture)
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Migration_FreshAndPreviousSchemaPreservePreferencesAndEnforceVersionAndUniqueness(bool upgrade)
    {
        // A new randomly named database only; never reset the developer database or retained volumes.
        var connection = new NpgsqlConnectionStringBuilder(fixture.ConnectionString)
        { Database = $"settings_migration_{Guid.NewGuid():N}" };
        var options = new DbContextOptionsBuilder<GodForgeDbContext>().UseNpgsql(connection.ConnectionString).UseSnakeCaseNamingConvention().Options;
        await using var db = new GodForgeDbContext(options);
        var now = DateTimeOffset.UtcNow;
        var user = User.Create($"migration-{Guid.NewGuid():N}@example.test", "Migration", "hash", now);
        var settingId = Guid.NewGuid();
        var org = Organization.Create("Migration org", $"migration-{Guid.NewGuid():N}", user.Id, now);
        var project = Project.Create(org.Id, "Preserved project", "preserved-project", "Non-default description", "4.4", ProjectVisibility.Internal, user.Id, now);
        try
        {
            if (upgrade)
            {
                await db.GetService<IMigrator>().MigrateAsync("20260814133000_AddTrigramSearchIndexes");
                db.Users.Add(user); db.Organizations.Add(org); db.Projects.Add(project); await db.SaveChangesAsync();
                await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO identity.user_settings (id,user_id,theme,notification_in_app,notification_email,notification_digest,created_at,updated_at) VALUES ({settingId},{user.Id},{"Dark"},{false},{true},{"Weekly"},{now},{now})");
            }
            await db.Database.MigrateAsync();
            Assert.Empty(await db.Database.GetPendingMigrationsAsync());
            Assert.False(db.Database.HasPendingModelChanges());
            if (!upgrade)
            {
                db.Users.Add(user);
                var defaults = UserSetting.Create(user.Id, now);
                settingId = defaults.Id;
                db.UserSettings.Add(defaults); await db.SaveChangesAsync();
            }
            db.ChangeTracker.Clear();
            var setting = await db.UserSettings.SingleAsync(s => s.UserId == user.Id);
            Assert.Equal(settingId, setting.Id);
            Assert.Equal(1, setting.Version);
            Assert.Equal(upgrade ? Theme.Dark : Theme.Light, setting.Theme);
            Assert.Equal(upgrade ? NotificationDigest.Weekly : NotificationDigest.Off, setting.NotificationDigest);
            Assert.Equal(!upgrade, setting.NotificationInApp);
            Assert.True(setting.NotificationEmail);
            Assert.Equal(now.ToUnixTimeMilliseconds(), setting.CreatedAt.ToUnixTimeMilliseconds());
            Assert.Empty(await db.OrganizationSettings.ToListAsync());
            if (upgrade)
            {
                var preserved = await db.Projects.SingleAsync(p => p.Id == project.Id);
                Assert.Equal(org.Id, preserved.OrganizationId);
                Assert.Equal("Non-default description", preserved.Description);
                Assert.Equal("4.4", preserved.GodotVersion);
                Assert.Equal(ProjectVisibility.Internal, preserved.Visibility);
                Assert.Equal(1, preserved.Version);
            }
            else
            {
                db.Organizations.Add(org); await db.SaveChangesAsync();
            }
            db.OrganizationSettings.Add(OrganizationSetting.Create(org.Id, now));
            await db.SaveChangesAsync();
            await using var duplicatePolicy = new GodForgeDbContext(options);
            duplicatePolicy.OrganizationSettings.Add(OrganizationSetting.Create(org.Id, now));
            await Assert.ThrowsAsync<DbUpdateException>(() => duplicatePolicy.SaveChangesAsync());
            await using var invalidScope = new GodForgeDbContext(options);
            invalidScope.OrganizationSettings.Add(OrganizationSetting.Create(Guid.NewGuid(), now));
            await Assert.ThrowsAsync<DbUpdateException>(() => invalidScope.SaveChangesAsync());
            await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE core.organization_settings SET version=0 WHERE organization_id={org.Id}"));
            await using var policyFirst = new GodForgeDbContext(options);
            await using var policySecond = new GodForgeDbContext(options);
            var pa = await policyFirst.OrganizationSettings.SingleAsync(s => s.OrganizationId == org.Id);
            var pb = await policySecond.OrganizationSettings.SingleAsync(s => s.OrganizationId == org.Id);
            pa.Update(false, 1, now); pb.Update(true, 1, now);
            await policyFirst.SaveChangesAsync();
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => policySecond.SaveChangesAsync());

            // Independent contexts prove the EF token is enforced by PostgreSQL, beyond advisory locking.
            await using var first = new GodForgeDbContext(options);
            await using var second = new GodForgeDbContext(options);
            var a = await first.UserSettings.SingleAsync(s => s.UserId == user.Id);
            var b = await second.UserSettings.SingleAsync(s => s.UserId == user.Id);
            a.Update(Theme.Dark, true, false, NotificationDigest.Daily, 1, now);
            b.Update(Theme.Light, false, true, NotificationDigest.Off, 1, now);
            await first.SaveChangesAsync();
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());
            await using var duplicate = new GodForgeDbContext(options);
            duplicate.UserSettings.Add(UserSetting.Create(user.Id, now));
            await Assert.ThrowsAsync<DbUpdateException>(() => duplicate.SaveChangesAsync());
            await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE identity.user_settings SET version=0 WHERE user_id={user.Id}"));
        }
        finally
        {
            // The target is exclusively the random database created above.
            await db.Database.EnsureDeletedAsync();
        }
    }
}
