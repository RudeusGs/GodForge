using GodForge.Application.Common.Interfaces;
using GodForge.Domain.Entities;
using GodForge.Domain.Entities.Identity;
using GodForge.Domain.Enums;
using GodForge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GodForge.Infrastructure.Data;

public class SeedDataService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SeedDataService> _logger;

    public SeedDataService(IServiceProvider serviceProvider, IConfiguration configuration, ILogger<SeedDataService> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GodForgeDbContext>();
        
        // This is primarily for development to ensure the schema exists.
        // In production, migrations should be run via CLI or separate task.
        bool.TryParse(_configuration["RunMigrationsOnStartup"], out var runMigrations);
        if (runMigrations)
        {
            _logger.LogInformation("Applying migrations...");
            await context.Database.MigrateAsync(cancellationToken);
        }

        var seedAdminEmail = _configuration["SeedAdmin:Email"];
        var seedAdminPassword = _configuration["SeedAdmin:Password"];

        if (!string.IsNullOrEmpty(seedAdminEmail) && !string.IsNullOrEmpty(seedAdminPassword))
        {
            if (!await context.Users.AnyAsync(u => u.NormalizedEmail == seedAdminEmail.ToUpperInvariant(), cancellationToken))
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
                
                _logger.LogInformation("Seeded admin user: {Email}", seedAdminEmail);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
