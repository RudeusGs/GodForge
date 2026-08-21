using GodForge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GodForge.IntegrationTests.Persistence;

[CollectionDefinition(CollectionName, DisableParallelization = true)]
public sealed class PostgresPersistenceCollection : ICollectionFixture<PostgresPersistenceFixture>
{
    public const string CollectionName = "PostgreSQL persistence";
}

public sealed class PostgresPersistenceFixture : IAsyncLifetime
{
    public string ConnectionString { get; } = Environment.GetEnvironmentVariable("GODFORGE_TEST_POSTGRES")
        ?? "Host=localhost;Port=5433;Database=godforge_test;Username=godforge;Password=godforge";

    public GodForgeDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<GodForgeDbContext>()
            .UseNpgsql(ConnectionString)
            .UseSnakeCaseNamingConvention()
            .Options;
        return new GodForgeDbContext(options);
    }

    public async Task InitializeAsync()
    {
        await using var context = CreateContext();
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await using var context = CreateContext();
        await context.Database.EnsureDeletedAsync();
    }
}
