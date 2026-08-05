using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Infrastructure.AI;
using GodForge.Infrastructure.Analysis;
using GodForge.Infrastructure.Auditing;
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
        services.AddScoped<IAuthChallengeRepository, AuthChallengeRepository>();
        services.AddScoped<IUserSessionRepository, UserSessionRepository>();
        services.AddScoped<IUserInviteRepository, UserInviteRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<IOrganizationMemberRepository, OrganizationMemberRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IProjectMemberRepository, ProjectMemberRepository>();
        services.AddScoped<IGitRepositoryRepository, GitRepositoryRepository>();
        services.AddScoped<IRepositorySnapshotRepository, RepositorySnapshotRepository>();
        services.AddScoped<IAiAnalysisRepository, AiAnalysisRepository>();
        services.AddScoped<IHealthReportRepository, HealthReportRepository>();
        services.AddScoped<IIdempotencyRepository, IdempotencyRepository>();
        services.AddScoped<IDependencyGraphSnapshotRepository, DependencyGraphSnapshotRepository>();
        services.AddScoped<IAnalysisRunRepository, AnalysisRunRepository>();
        services.AddScoped<IJobRepository, JobRepository>();
        services.AddScoped<IActivityRepository, ActivityRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();

        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IM1QuotaPolicy, M1QuotaPolicy>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IActivityWriter, ActivityWriter>();
        services.AddScoped<IAuditWriter, AuditWriter>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddSingleton<IFrontendUrlBuilder, FrontendUrlBuilder>();
        services.AddSingleton<IJobPublisher, RabbitMqJobPublisher>();
        services.AddScoped<IOutboxWriter, OutboxWriter>();
        services.AddScoped<IEmailOutbox, EmailOutbox>();

        services.AddOptions<OutboxEncryptionSettings>()
            .Bind(configuration.GetSection(OutboxEncryptionSettings.SectionName))
            .ValidateDataAnnotations()
            .Validate(
                settings => !string.Equals(settings.Key, configuration["Jwt:Secret"], StringComparison.Ordinal),
                "Outbox encryption key must be different from the JWT signing secret.")
            .ValidateOnStart();
        services.Configure<EmailSettings>(configuration.GetSection("Email"));
        services
            .AddOptions<RabbitMqSettings>()
            .Bind(configuration.GetSection(RabbitMqSettings.SectionName))
            .ValidateDataAnnotations()
            .Validate(
                settings =>
                    !settings.Enabled ||
                    !string.IsNullOrWhiteSpace(settings.HostName),
                "RabbitMQ HostName is required when RabbitMQ is enabled.")
            .Validate(
                settings =>
                    !settings.Enabled ||
                    !string.IsNullOrWhiteSpace(settings.UserName),
                "RabbitMQ UserName is required when RabbitMQ is enabled.")
            .Validate(
                settings =>
                    !settings.Enabled ||
                    !string.IsNullOrWhiteSpace(settings.Password),
                "RabbitMQ Password is required when RabbitMQ is enabled.")
            .Validate(
                settings =>
                    !settings.Enabled ||
                    !string.IsNullOrWhiteSpace(settings.VirtualHost),
                "RabbitMQ VirtualHost is required when RabbitMQ is enabled.")
            .Validate(
                settings =>
                    !settings.Enabled ||
                    !(
                        string.Equals(
                            settings.UserName,
                            "guest",
                            StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(
                            settings.Password,
                            "guest",
                            StringComparison.Ordinal)
                    ),
                "RabbitMQ guest/guest credentials are not allowed.")
            .ValidateOnStart();
        services.AddOptions<RepositoryProcessingSettings>()
            .Bind(configuration.GetSection("RepositoryProcessing"))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.Configure<GeminiSettings>(configuration.GetSection("Gemini"));
        services.Configure<ForgejoSettings>(configuration.GetSection("Forgejo"));
        services.AddOptions<M1QuotaSettings>()
            .Bind(configuration.GetSection(M1QuotaSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
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
        services.AddScoped<IDependencyGraphBuilder, DependencyGraphBuilder>();
        services.AddSingleton<IRepositoryLockProvider, PostgresRepositoryLockProvider>();
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

    public static IServiceCollection AddIdentityInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddSingleton<ISecretHashService, SecretHashService>();
        services.AddOptions<JwtSettings>()
            .Bind(configuration.GetSection("Jwt"))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        return services;
    }

    public static IServiceCollection AddOutboxDispatching(this IServiceCollection services)
    {
        services.AddHostedService<OutboxDispatcherService>();
        return services;
    }
}
