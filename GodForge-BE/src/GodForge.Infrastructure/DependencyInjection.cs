using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Infrastructure.AI;
using GodForge.Infrastructure.Analysis;
using GodForge.Infrastructure.Configuration;
using GodForge.Infrastructure.Git;
using GodForge.Infrastructure.HostedGit;
using GodForge.Infrastructure.Messaging;
using GodForge.Infrastructure.Persistence;
using GodForge.Infrastructure.Persistence.Repositories;
using GodForge.Infrastructure.Security;
using GodForge.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
        services.AddScoped<IRepositorySnapshotRepository, RepositorySnapshotRepository>();
        services.AddScoped<IAiAnalysisRepository, AiAnalysisRepository>();
        services.AddScoped<IJobRepository, JobRepository>();
        services.AddScoped<IActivityRepository, ActivityRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();

        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IActivityWriter, ActivityWriter>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddSingleton<IFrontendUrlBuilder, FrontendUrlBuilder>();
        services.AddScoped<IJobPublisher, RabbitMqJobPublisher>();

        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));
        services.AddOptions<JwtSettings>()
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.Configure<EmailSettings>(configuration.GetSection("Email"));
        services.Configure<RabbitMqSettings>(configuration.GetSection("RabbitMQ"));
        services.AddOptions<RepositoryProcessingSettings>()
            .Bind(configuration.GetSection("RepositoryProcessing"))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.Configure<GeminiSettings>(configuration.GetSection("Gemini"));
        services.Configure<ForgejoSettings>(configuration.GetSection("Forgejo"));
        services.AddOptions<FrontendSettings>()
            .Bind(configuration.GetSection("Frontend"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var redisConfiguration = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisConfiguration))
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

        services.AddSingleton<ISecretRedactor, SecretRedactor>();
        services.AddScoped<IRepositoryContextBuilder, RepositoryContextBuilder>();
        services.AddScoped<IDeterministicProjectAnalyzer, DeterministicProjectAnalyzer>();
        services.AddScoped<IRepositoryWorkspaceService, GitWorkspaceService>();

        services.AddHttpClient<IAiAnalysisProvider, GeminiAnalysisProvider>((serviceProvider, client) =>
        {
            var settings = serviceProvider.GetRequiredService<IOptions<GeminiSettings>>().Value;
            client.BaseAddress = new Uri(settings.Endpoint.TrimEnd('/') + "/", UriKind.Absolute);
            client.Timeout = Timeout.InfiniteTimeSpan;
        });

        services.AddHttpClient<IHostedGitService, ForgejoHostedGitService>((serviceProvider, client) =>
        {
            var settings = serviceProvider.GetRequiredService<IOptions<ForgejoSettings>>().Value;
            client.BaseAddress = new Uri(settings.BaseUrl.TrimEnd('/') + "/", UriKind.Absolute);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        return services;
    }
}

