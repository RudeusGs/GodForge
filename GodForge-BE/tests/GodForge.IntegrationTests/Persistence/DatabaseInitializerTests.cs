using System.Data;
using GodForge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GodForge.IntegrationTests.Persistence;

[Collection(PostgresPersistenceCollection.CollectionName)]
public sealed class DatabaseInitializerTests
{
    private readonly PostgresPersistenceFixture _fixture;

    public DatabaseInitializerTests(PostgresPersistenceFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task InitializeGodForgeDatabaseAsync_EmptyDatabase_AppliesAllMigrationsAndRawSqlIndexes()
    {
        await using (var resetContext = _fixture.CreateContext())
        {
            await resetContext.Database.OpenConnectionAsync();
            try
            {
                await using var resetCommand = resetContext.Database.GetDbConnection().CreateCommand();
                resetCommand.CommandText = """
                    DROP SCHEMA IF EXISTS admin, analysis, audit, collab, core, governance,
                        identity, metadata, ops, repo, search, storage CASCADE;
                    DROP TABLE IF EXISTS public."__EFMigrationsHistory";
                    """;
                resetCommand.CommandType = CommandType.Text;
                await resetCommand.ExecuteNonQueryAsync();
            }
            finally
            {
                await resetContext.Database.CloseConnectionAsync();
            }
        }

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<GodForgeDbContext>(options =>
            options.UseNpgsql(_fixture.ConnectionString)
                .UseSnakeCaseNamingConvention());

        await using var provider = services.BuildServiceProvider();
        await provider.InitializeGodForgeDatabaseAsync();

        await using var verificationContext = _fixture.CreateContext();
        Assert.Empty(await verificationContext.Database.GetPendingMigrationsAsync());

        await verificationContext.Database.OpenConnectionAsync();
        try
        {
            await using var command = verificationContext.Database.GetDbConnection().CreateCommand();
            command.CommandText = """
                SELECT EXISTS (
                    SELECT 1
                    FROM pg_indexes
                    WHERE schemaname = 'core'
                      AND indexname = 'ux_projects_org_upper_name_active'
                );
                """;
            command.CommandType = CommandType.Text;
            var indexExists = await command.ExecuteScalarAsync();
            Assert.True(indexExists is true);
        }
        finally
        {
            await verificationContext.Database.CloseConnectionAsync();
        }
    }
}
