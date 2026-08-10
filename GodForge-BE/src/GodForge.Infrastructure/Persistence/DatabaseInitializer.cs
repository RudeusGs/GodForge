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

            if (!await MigrationHistoryExistsAsync(context, cancellationToken) &&
                await HasApplicationTablesAsync(context, cancellationToken))
            {
                throw new InvalidOperationException(
                    "The database contains application tables but has no EF migration history. " +
                    "Back up the database and establish a migration baseline before starting GodForge.");
            }

            var pendingMigrations = (await context.Database.GetPendingMigrationsAsync(cancellationToken)).ToArray();
            if (pendingMigrations.Length == 0)
            {
                logger.LogInformation("GodForge database schema is already up to date");
                return;
            }

            logger.LogInformation(
                "Applying {MigrationCount} pending GodForge database migration(s)",
                pendingMigrations.Length);

            // Always build relational databases through EF migrations. EnsureCreated bypasses
            // migration operations such as raw SQL, filtered/functional indexes and data fixes.
            await context.Database.MigrateAsync(cancellationToken);

            logger.LogInformation(
                "GodForge database migrations applied successfully; latest migration is {MigrationId}",
                pendingMigrations[^1]);
        }
        finally
        {
            try
            {
                if (context.Database.GetDbConnection().State == ConnectionState.Open)
                {
                    await ExecuteScalarAsync(
                        context,
                        $"SELECT pg_advisory_unlock({BootstrapAdvisoryLockId});",
                        CancellationToken.None);
                }
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
}
