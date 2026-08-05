using System.Text;
using System.Threading.RateLimiting;
using GodForge.Api.Services;
using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Interfaces.Repositories;
using GodForge.Domain.Enums;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
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
        services.AddScoped<IRequestContext, HttpRequestContext>();
        services.AddSingleton<RefreshTokenCookieService>();

        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Title = "GodForge API",
                Version = "v1",
                Description = "GodForge Platform API Documentation"
            });

            c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                Name = "Authorization",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement()
            {
                {
                    new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                    {
                        Reference = new Microsoft.OpenApi.Models.OpenApiReference
                        {
                            Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        },
                        Scheme = "oauth2",
                        Name = "Bearer",
                        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    },
                    new List<string>()
                }
            });

            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = System.IO.Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (System.IO.File.Exists(xmlPath))
            {
                c.IncludeXmlComments(xmlPath);
            }
        });

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
                    var principal = context.Principal;
                    var userIdValue = principal?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                        ?? principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    var securityStamp = principal?.FindFirst("security_stamp")?.Value;
                    var sessionIdValue = principal?.FindFirst("sid")?.Value;

                    if (!Guid.TryParse(userIdValue, out var userId) ||
                        !Guid.TryParse(sessionIdValue, out var sessionId) ||
                        string.IsNullOrWhiteSpace(securityStamp))
                    {
                        context.Fail("The token is missing required identity claims.");
                        return;
                    }

                    var userRepository = context.HttpContext.RequestServices.GetRequiredService<IUserRepository>();
                    var sessionRepository = context.HttpContext.RequestServices.GetRequiredService<IUserSessionRepository>();
                    var clock = context.HttpContext.RequestServices.GetRequiredService<IClock>();
                    var user = await userRepository.GetByIdAsync(userId, context.HttpContext.RequestAborted);
                    var session = await sessionRepository.GetActiveAsync(sessionId, userId, context.HttpContext.RequestAborted);
                    if (user is null ||
                        user.DeletedAt is not null ||
                        user.Status != UserStatus.Active ||
                        !string.Equals(user.SecurityStamp, securityStamp, StringComparison.Ordinal) ||
                        session is null ||
                        !session.IsActive(clock.UtcNow))
                    {
                        context.Fail("The token is no longer valid for this account or session.");
                        return;
                    }

                    var blacklistService = context.HttpContext.RequestServices.GetRequiredService<ITokenBlacklistService>();
                    var jti = principal?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti)?.Value;
                    if (!string.IsNullOrEmpty(jti) &&
                        await blacklistService.IsBlacklistedAsync(jti, context.HttpContext.RequestAborted))
                    {
                        context.Fail("This token has been revoked.");
                    }
                },
                OnChallenge = async context =>
                {
                    context.HandleResponse();
                    await ApiErrorResponseWriter.WriteAsync(
                        context.HttpContext,
                        StatusCodes.Status401Unauthorized,
                        "UNAUTHORIZED",
                        "Authentication is required.",
                        context.HttpContext.RequestAborted);
                },
                OnForbidden = context => ApiErrorResponseWriter.WriteAsync(
                    context.HttpContext,
                    StatusCodes.Status403Forbidden,
                    "SECURITY_FORBIDDEN",
                    "You do not have permission to perform this operation.",
                    context.HttpContext.RequestAborted)
            };
        });

        services.AddAuthorization();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = (context, cancellationToken) => new ValueTask(
                ApiErrorResponseWriter.WriteAsync(
                    context.HttpContext,
                    StatusCodes.Status429TooManyRequests,
                    "RATE_LIMIT_EXCEEDED",
                    "Too many requests. Please try again later.",
                    cancellationToken));
            options.AddPolicy("auth-sensitive", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    static _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    }));
            options.AddPolicy("auth-otp", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    static _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 3,
                        Window = TimeSpan.FromMinutes(5),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    }));
        });

        var frontendBaseUrl = configuration["Frontend:BaseUrl"] ?? "http://localhost:5173";
        var frontendOrigin = Uri.TryCreate(frontendBaseUrl, UriKind.Absolute, out var frontendUri)
            ? frontendUri.GetLeftPart(UriPartial.Authority)
            : "http://localhost:5173";

        services.AddCors(options =>
        {
            options.AddPolicy("CorsPolicy", policy =>
            {
                policy.WithOrigins(frontendOrigin)
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            });
        });

        return services;
    }
}
