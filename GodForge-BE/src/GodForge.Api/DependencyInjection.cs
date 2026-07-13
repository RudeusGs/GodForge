using System.Text;
using GodForge.Api.Services;
using GodForge.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace GodForge.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();



        // Configure JWT Authentication
        var jwtSettings = configuration.GetSection("Jwt").Get<GodForge.Infrastructure.Configuration.JwtSettings>() ?? new GodForge.Infrastructure.Configuration.JwtSettings();
        var jwtSecret = string.IsNullOrEmpty(jwtSettings.Secret) ? "A_TEMPORARY_KEY_FOR_REGISTRATION_THAT_WILL_FAIL_STARTUP_VALIDATION123!" : jwtSettings.Secret;
        var key = Encoding.UTF8.GetBytes(jwtSecret);

        services.AddAuthentication(x =>
        {
            x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(x =>
        {
            x.RequireHttpsMetadata = !environment.IsDevelopment();
            x.SaveToken = true;
            x.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
            x.Events = new JwtBearerEvents
            {
                OnTokenValidated = async context =>
                {
                    var blacklistService = context.HttpContext.RequestServices.GetRequiredService<ITokenBlacklistService>();
                    var jti = context.Principal?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti)?.Value;
                    if (!string.IsNullOrEmpty(jti) && await blacklistService.IsBlacklistedAsync(jti))
                    {
                        context.Fail("This token has been blacklisted.");
                    }
                }
            };
        });

        services.AddAuthorization();

        // Add CORS Policy for Frontend Development Server
        services.AddCors(options =>
        {
            options.AddPolicy("CorsPolicy", policy =>
            {
                policy.WithOrigins("http://localhost:5173")
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            });
        });

        return services;
    }
}
