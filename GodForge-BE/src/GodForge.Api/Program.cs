using GodForge.Api;
using GodForge.Api.HealthChecks;
using GodForge.Api.Middleware;
using GodForge.Application;
using GodForge.Infrastructure;
using GodForge.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

// Load environment variables from .env file
DotNetEnv.Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

// Add GodForge Layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddIdentityInfrastructure(builder.Configuration);
builder.Services.AddApiServices(builder.Configuration, builder.Environment);
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"])
    .AddCheck<CacheHealthCheck>("cache", tags: ["ready"])
    .AddCheck<RabbitMqHealthCheck>("rabbitmq", tags: ["ready"]);

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    await app.Services.InitializeGodForgeDatabaseAsync();
}

// Configure the HTTP request pipeline.
// Resolve the effective client address before authentication metadata and rate limiting.
// ForwardedHeadersOptions trusts only explicitly configured proxies/networks (plus the
// framework loopback defaults); arbitrary client-supplied X-Forwarded-For is ignored.
app.UseForwardedHeaders();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "GodForge API V1");
        c.RoutePrefix = "swagger";
    });
}

// This wrapper must remain outside exception handling so logout cookie cleanup is
// applied after both success and sanitized failure responses are constructed.
app.UseMiddleware<AuthLogoutCookieCleanupMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<CorrelationIdMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("CorsPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready")
});
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready")
});
app.MapControllers();
if (app.Environment.IsDevelopment())
{
    app.MapGet("/", context =>
    {
        context.Response.Redirect("/swagger");
        return Task.CompletedTask;
    });
}

app.Run();

public partial class Program { }
