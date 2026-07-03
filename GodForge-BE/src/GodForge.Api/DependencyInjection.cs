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
        var jwtSecret = configuration["Jwt:Secret"];

        if (!environment.IsDevelopment())
        {
            if (string.IsNullOrWhiteSpace(jwtSecret) || jwtSecret.Length < 32 || jwtSecret == "SuperSecretKeyForDevelopmentOnlyPleaseChangeInProduction123!")
            {
                throw new InvalidOperationException("A secure Jwt:Secret of at least 32 characters is required in non-development environments.");
            }
        }

        jwtSecret ??= "SuperSecretKeyForDevelopmentOnlyPleaseChangeInProduction123!";
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
                ValidIssuer = configuration["Jwt:Issuer"] ?? "GodForge",
                ValidateAudience = true,
                ValidAudience = configuration["Jwt:Audience"] ?? "GodForgeUsers",
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

        return services;
    }
}
