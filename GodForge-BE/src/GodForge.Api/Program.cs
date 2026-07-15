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
builder.Services.AddApiServices(builder.Configuration, builder.Environment);
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"])
    .AddCheck<CacheHealthCheck>("cache", tags: ["ready"])
    .AddCheck<RabbitMqHealthCheck>("rabbitmq", tags: ["ready"]);

var app = builder.Build();
await app.Services.InitializeGodForgeDatabaseAsync();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "GodForge API V1");
        c.RoutePrefix = "swagger";
    });
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<CorrelationIdMiddleware>();

app.UseHttpsRedirection();

app.UseCors("CorsPolicy");
app.UseRateLimiter();

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
app.MapGet("/", context =>
{
    context.Response.Redirect("/swagger");
    return Task.CompletedTask;
});

app.Run();

public partial class Program { }
