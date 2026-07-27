using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GodForge.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    private const long BootstrapAdvisoryLockId = 4_746_646_672_019;

    public static async Task InitializeGodForgeDatabaseAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GodForgeDbContext>();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("GodForge.DatabaseInitializer");

        if (!context.Database.IsRelational())
        {
            await context.Database.EnsureCreatedAsync(cancellationToken);
            return;
        }

        await context.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            await ExecuteScalarAsync(
                context,
                $"SELECT pg_advisory_lock({BootstrapAdvisoryLockId});",
                cancellationToken);

            if (await MigrationHistoryExistsAsync(context, cancellationToken))
            {
                await context.Database.MigrateAsync(cancellationToken);
                return;
            }

            if (await HasApplicationTablesAsync(context, cancellationToken))
            {
                throw new InvalidOperationException(
                    "The database contains application tables but has no EF migration history. " +
                    "Back up the database and establish a migration baseline before starting GodForge.");
            }

            logger.LogInformation("Initializing an empty GodForge database from the current EF model");
            await context.Database.EnsureCreatedAsync(cancellationToken);
            await CreateMigrationHistoryAsync(context, cancellationToken);

            var migrations = context.Database.GetMigrations().ToArray();
            if (migrations.Length == 0)
            {
                throw new InvalidOperationException("No EF migrations were discovered while creating the database baseline.");
            }

            foreach (var migration in migrations)
            {
                await RecordMigrationAsync(context, migration, cancellationToken);
            }

            logger.LogInformation(
                "GodForge database baseline initialized with {MigrationCount} recorded migrations",
                migrations.Length);
        }
        finally
        {
            try
            {
                await ExecuteScalarAsync(
                    context,
                    $"SELECT pg_advisory_unlock({BootstrapAdvisoryLockId});",
                    cancellationToken);
            }
            finally
            {
                await context.Database.CloseConnectionAsync();
            }
        }
    }

    private static async Task<bool> MigrationHistoryExistsAsync(
        GodForgeDbContext context,
        CancellationToken cancellationToken)
    {
        var result = await ExecuteScalarAsync(
            context,
            "SELECT to_regclass('public.\"__EFMigrationsHistory\"') IS NOT NULL;",
            cancellationToken);
        return result is true;
    }

    private static async Task<bool> HasApplicationTablesAsync(
        GodForgeDbContext context,
        CancellationToken cancellationToken)
    {
        var result = await ExecuteScalarAsync(
            context,
            """
            SELECT EXISTS (
                SELECT 1
                FROM information_schema.tables
                WHERE table_type = 'BASE TABLE'
                  AND table_schema NOT IN ('pg_catalog', 'information_schema')
                  AND table_name <> '__EFMigrationsHistory'
            );
            """,
            cancellationToken);
        return result is true;
    }

    private static async Task CreateMigrationHistoryAsync(
        GodForgeDbContext context,
        CancellationToken cancellationToken)
    {
        await ExecuteNonQueryAsync(
            context,
            """
            CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
                "MigrationId" character varying(150) NOT NULL,
                "ProductVersion" character varying(32) NOT NULL,
                CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
            );
            """,
            cancellationToken);
    }

    private static async Task RecordMigrationAsync(
        GodForgeDbContext context,
        string migrationId,
        CancellationToken cancellationToken)
    {
        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = """
            INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
            VALUES (@migrationId, @productVersion)
            ON CONFLICT ("MigrationId") DO NOTHING;
            """;
        command.CommandType = CommandType.Text;

        var migrationParameter = command.CreateParameter();
        migrationParameter.ParameterName = "migrationId";
        migrationParameter.Value = migrationId;
        command.Parameters.Add(migrationParameter);

        var productVersionParameter = command.CreateParameter();
        productVersionParameter.ParameterName = "productVersion";
        productVersionParameter.Value = "9.0.1";
        command.Parameters.Add(productVersionParameter);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<object?> ExecuteScalarAsync(
        GodForgeDbContext context,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;
        command.CommandType = CommandType.Text;
        return await command.ExecuteScalarAsync(cancellationToken);
    }

    private static async Task ExecuteNonQueryAsync(
        GodForgeDbContext context,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;
        command.CommandType = CommandType.Text;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
