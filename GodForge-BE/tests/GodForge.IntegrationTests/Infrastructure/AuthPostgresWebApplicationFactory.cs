using System.Text;
using GodForge.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace GodForge.IntegrationTests.Infrastructure;

public sealed class AuthPostgresWebApplicationFactory(string connectionString) : WebApplicationFactory<Program>
{
    private const string JwtSecret = "auth-http-test-signing-key-64-characters-minimum-000000000000000";
    private const string JwtIssuer = "GodForge.AuthHttpTests";

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
                ["ConnectionStrings:DefaultConnection"] = connectionString,
                ["ConnectionStrings:Redis"] = string.Empty,
                ["Jwt:Secret"] = JwtSecret,
                ["Jwt:Issuer"] = JwtIssuer,
                ["Jwt:Audience"] = JwtIssuer,
                ["OutboxEncryption:Key"] = "auth-http-test-outbox-key-64-characters-minimum-0000000000000",
                ["SecretHashing:Key"] = "auth-http-test-secret-hash-key-64-characters-minimum-000000000",
                ["Frontend:BaseUrl"] = "https://frontend.auth-http.test",
                ["RabbitMQ:Enabled"] = "false",
                ["M1Quotas:MaxActiveSessionsPerUser"] = "10"
            });
        });
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IDistributedAuthRateLimiter>();
            services.AddSingleton<IDistributedAuthRateLimiter, DevelopmentAuthRateLimiter>();
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters.IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret));
                options.TokenValidationParameters.ValidIssuer = JwtIssuer;
                options.TokenValidationParameters.ValidAudience = JwtIssuer;
            });
            foreach (var descriptor in services
                         .Where(descriptor => descriptor.ServiceType == typeof(IHostedService))
                         .ToList())
            {
                services.Remove(descriptor);
            }
        });
    }
}
