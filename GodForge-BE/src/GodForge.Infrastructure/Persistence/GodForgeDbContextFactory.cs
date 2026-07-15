using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GodForge.Infrastructure.Persistence;

public sealed class GodForgeDbContextFactory : IDesignTimeDbContextFactory<GodForgeDbContext>
{
    public GodForgeDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=godforge;Username=godforge;Password=godforge_local_password";

        var options = new DbContextOptionsBuilder<GodForgeDbContext>()
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        return new GodForgeDbContext(options);
    }
}
