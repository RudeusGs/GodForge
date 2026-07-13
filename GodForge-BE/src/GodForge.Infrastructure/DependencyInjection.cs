using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Infrastructure.Configuration;
using GodForge.Infrastructure.Messaging;
using GodForge.Infrastructure.Persistence;
using GodForge.Infrastructure.Persistence.Repositories;
using GodForge.Infrastructure.Security;
using GodForge.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GodForge.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<GodForgeDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"))
                   .UseSnakeCaseNamingConvention());

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IProjectMemberRepository, ProjectMemberRepository>();
        services.AddScoped<IGitRepositoryRepository, GitRepositoryRepository>();
        services.AddScoped<IJobRepository, JobRepository>();
        services.AddScoped<IActivityRepository, ActivityRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();

        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IActivityWriter, ActivityWriter>();
        services.AddScoped<IJobPublisher, StubJobPublisher>();
        
        // Email Configuration and Service
        services.Configure<EmailSettings>(options =>
        {
            options.Smtp.Host = configuration["Email:Smtp:Host"] ?? string.Empty;
            
            var portSection = configuration["Email:Smtp:Port"];
            options.Smtp.Port = string.IsNullOrEmpty(portSection) ? 587 : int.Parse(portSection);
            
            var sslSection = configuration["Email:Smtp:EnableSsl"];
            options.Smtp.EnableSsl = !string.IsNullOrEmpty(sslSection) && bool.Parse(sslSection);
            
            options.Smtp.UserName = configuration["Email:Smtp:UserName"] ?? string.Empty;
            options.Smtp.Password = configuration["Email:Smtp:Password"] ?? string.Empty;
            options.Smtp.FromEmail = configuration["Email:Smtp:FromEmail"] ?? string.Empty;
            options.Smtp.FromName = configuration["Email:Smtp:FromName"] ?? "GodForge";
        });
        services.AddScoped<IEmailService, EmailService>();

        var redisConfiguration = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConfiguration))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConfiguration;
                options.InstanceName = "GodForge:";
            });
        }
        else
        {
            services.AddDistributedMemoryCache();
        }

        services.AddMemoryCache();
        services.AddSingleton<ICacheService, GodForge.Infrastructure.Caching.RedisCacheService>();
        services.AddScoped<ITokenBlacklistService, TokenBlacklistService>();
        return services;
    }
}
