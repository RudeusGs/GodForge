using GodForge.Application.Common.Interfaces;
using GodForge.Domain.Entities.Identity;
using GodForge.Domain.Enums;
using GodForge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GodForge.Infrastructure.Initialization;

public sealed class DevelopmentAdminSeeder : IAdminSeeder
{
    private readonly GodForgeDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<DevelopmentAdminSeeder> _logger;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IClock _clock;

    public DevelopmentAdminSeeder(
        GodForgeDbContext context,
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger<DevelopmentAdminSeeder> logger,
        IPasswordHasher passwordHasher,
        IClock clock)
    {
        _context = context;
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
        _passwordHasher = passwordHasher;
        _clock = clock;
    }

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        if (!_environment.IsDevelopment())
        {
            var defaultConnection = _configuration.GetConnectionString("DefaultConnection");
            if (defaultConnection != null && defaultConnection.Contains("REPLACE_ME"))
            {
                throw new InvalidOperationException("Production configuration contains placeholder database credentials. Startup aborted.");
            }
            return;
        }

        bool.TryParse(_configuration["RunMigrationsOnStartup"], out var runMigrations);
        if (runMigrations)
        {
            _logger.LogInformation("Applying migrations for Development environment...");
            await _context.Database.MigrateAsync(cancellationToken);
        }

        bool.TryParse(_configuration["SeedAdmin:Enabled"], out var isSeedEnabled);
        if (!isSeedEnabled)
        {
            return;
        }

        var seedAdminEmail = _configuration["SeedAdmin:Email"];
        var seedAdminPassword = _configuration["SeedAdmin:Password"];

        if (string.IsNullOrWhiteSpace(seedAdminEmail) || string.IsNullOrWhiteSpace(seedAdminPassword) || seedAdminPassword == "REPLACE_ME_IN_ENV")
        {
            _logger.LogWarning("SeedAdmin is enabled but email or password is missing/placeholder. Skipping admin seed.");
            return;
        }

        if (!await _context.Users.AnyAsync(cancellationToken))
        {
            var passwordHash = _passwordHasher.HashPassword(seedAdminPassword);

            var admin = User.Create(
                seedAdminEmail,
                "System Admin",
                passwordHash,
                SystemRole.SystemAdmin,
                _clock.UtcNow);

            _context.Users.Add(admin);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Seeded development admin user: {Email}", seedAdminEmail);
        }
    }
}
