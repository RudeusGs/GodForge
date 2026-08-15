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
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
        services.AddOptions<OutboxDispatcherSettings>()
            .Bind(configuration.GetSection(OutboxDispatcherSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddOptions<EmailSettings>()
            .Bind(configuration.GetSection("Email"))
            .ValidateDataAnnotations()
            .Validate(
                settings => settings.Smtp.IsUnconfigured ||
                            (!string.IsNullOrWhiteSpace(settings.Smtp.Host) &&
                             !string.IsNullOrWhiteSpace(settings.Smtp.FromEmail) &&
                             !string.IsNullOrWhiteSpace(settings.Smtp.FromName)),
                "SMTP configuration must be either omitted or include Host, FromEmail, and FromName.")
            .Validate(
                settings => settings.Smtp.IsUnconfigured ||
                            System.Net.Mail.MailAddress.TryCreate(settings.Smtp.FromEmail, out _),
                "SMTP FromEmail must be a valid email address.")
            .ValidateOnStart();
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
        services.AddOptions<GeminiSettings>()
            .Bind(configuration.GetSection("Gemini"))
            .ValidateDataAnnotations()
            .Validate(
                settings => Uri.TryCreate(settings.Endpoint, UriKind.Absolute, out var endpoint) &&
                            endpoint.Scheme == Uri.UriSchemeHttps,
                "Gemini Endpoint must be an absolute HTTPS URL.")
            .Validate(
                settings => !settings.Enabled ||
                            (!string.IsNullOrWhiteSpace(settings.ApiKey) && !string.IsNullOrWhiteSpace(settings.Model)),
                "Gemini ApiKey and Model are required when Gemini is enabled.")
            .ValidateOnStart();
        services.AddOptions<ForgejoSettings>()
            .Bind(configuration.GetSection("Forgejo"))
            .ValidateDataAnnotations()
            .Validate(
                settings => Uri.TryCreate(settings.BaseUrl, UriKind.Absolute, out _),
                "Forgejo BaseUrl must be an absolute URL.")
            .Validate(
                settings => !settings.Enabled ||
                            (Uri.TryCreate(settings.BaseUrl, UriKind.Absolute, out var baseUri) &&
                             (baseUri.Scheme == Uri.UriSchemeHttps ||
                              (baseUri.Scheme == Uri.UriSchemeHttp && baseUri.IsLoopback)) &&
                             !string.IsNullOrWhiteSpace(settings.ApiToken)),
                "Forgejo requires a non-empty ApiToken and an HTTPS BaseUrl; HTTP is allowed only for loopback development endpoints.")
            .ValidateOnStart();
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
        services.AddScoped<ISessionValidationService>(serviceProvider => new SessionValidationService(
            serviceProvider.GetRequiredService<IUserSessionRepository>(),
            serviceProvider.GetRequiredService<IDistributedCache>(),
            serviceProvider.GetRequiredService<IOptions<JwtSettings>>(),
            serviceProvider.GetRequiredService<ILogger<SessionValidationService>>(),
            enablePositiveCache: !string.IsNullOrWhiteSpace(redisConfiguration)));

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
        services.AddScoped<IAuditWriter, AuditWriter>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddSingleton<ISecretHashService, SecretHashService>();
        services.AddOptions<JwtSettings>()
            .Bind(configuration.GetSection("Jwt"))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddOptions<SecretHashSettings>()
            .Bind(configuration.GetSection(SecretHashSettings.SectionName))
            .ValidateDataAnnotations()
            .Validate(
                settings => !string.Equals(settings.Key, configuration["Jwt:Secret"], StringComparison.Ordinal),
                "SecretHashing key must be different from the JWT signing secret.")
            .Validate(
                settings => string.IsNullOrWhiteSpace(settings.LegacyKey) || settings.LegacyKey.Length >= 32,
                "SecretHashing legacy key must be empty or at least 32 characters.")
            .Validate(
                settings => string.IsNullOrWhiteSpace(settings.LegacyKey) ||
                            !string.Equals(settings.Key, settings.LegacyKey, StringComparison.Ordinal),
                "SecretHashing legacy key must differ from the active key.")
            .ValidateOnStart();
        return services;
    }

    public static IServiceCollection AddOutboxDispatching(this IServiceCollection services)
    {
        services.AddHostedService<OutboxDispatcherService>();
        return services;
    }
}
