using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GodForge.IntegrationTests.Infrastructure;

public sealed class PostgresWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public PostgresWebApplicationFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddDebug();
        });
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _connectionString,
                ["ConnectionStrings:Redis"] = string.Empty,
                ["Jwt:Secret"] = "persistence-api-test-signing-key-64-characters-minimum-000000000000",
                ["Jwt:Issuer"] = "GodForge.PersistenceApiTests",
                ["Jwt:Audience"] = "GodForge.PersistenceApiTests",
                ["OutboxEncryption:Key"] = "persistence-api-test-outbox-key-64-characters-minimum-00000000000",
                ["SecretHashing:Key"] = "persistence-api-test-secret-hash-key-64-characters-minimum-000000",
                ["Frontend:BaseUrl"] = "https://frontend.persistence.test",
                ["RabbitMQ:Enabled"] = "false"
            });
        });
        builder.ConfigureServices(services =>
        {
            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });

            foreach (var descriptor in services
                         .Where(descriptor => descriptor.ServiceType == typeof(IHostedService))
                         .ToList())
            {
                services.Remove(descriptor);
            }
        });
    }
}
