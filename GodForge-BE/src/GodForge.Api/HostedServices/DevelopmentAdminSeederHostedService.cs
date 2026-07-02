using GodForge.Application.Common.Interfaces;
using GodForge.Domain.Entities.Identity;
using GodForge.Domain.Enums;
using GodForge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GodForge.Api.HostedServices;

public sealed class DevelopmentAdminSeederHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<DevelopmentAdminSeederHostedService> _logger;

    public DevelopmentAdminSeederHostedService(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger<DevelopmentAdminSeederHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
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

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GodForgeDbContext>();

        bool.TryParse(_configuration["RunMigrationsOnStartup"], out var runMigrations);
        if (runMigrations)
        {
            _logger.LogInformation("Applying migrations for Development environment...");
            await context.Database.MigrateAsync(cancellationToken);
        }

        var isSeedEnabled = _configuration.GetValue<bool>("SeedAdmin:Enabled");
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

        if (!await context.Users.AnyAsync(cancellationToken))
        {
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            var clock = scope.ServiceProvider.GetRequiredService<IClock>();

            var passwordHash = hasher.HashPassword(seedAdminPassword);

            var admin = User.Create(
                seedAdminEmail,
                "System Admin",
                passwordHash,
                SystemRole.SystemAdmin,
                clock.UtcNow);

            context.Users.Add(admin);
            await context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Seeded development admin user: {Email}", seedAdminEmail);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
