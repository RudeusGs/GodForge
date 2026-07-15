using GodForge.Infrastructure.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace GodForge.Api.HealthChecks;

public sealed class RabbitMqHealthCheck : IHealthCheck
{
    private readonly RabbitMqSettings _settings;

    public RabbitMqHealthCheck(IOptions<RabbitMqSettings> settings)
    {
        _settings = settings.Value;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_settings.Enabled)
        {
            return Task.FromResult(HealthCheckResult.Healthy("RabbitMQ is disabled by configuration."));
        }

        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _settings.HostName,
                Port = _settings.Port,
                UserName = _settings.UserName,
                Password = _settings.Password,
                VirtualHost = _settings.VirtualHost,
                RequestedConnectionTimeout = TimeSpan.FromSeconds(3)
            };

            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();
            return Task.FromResult(channel.IsOpen
                ? HealthCheckResult.Healthy("RabbitMQ is reachable.")
                : HealthCheckResult.Unhealthy("RabbitMQ channel could not be opened."));
        }
        catch (Exception exception)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("RabbitMQ health check failed.", exception));
        }
    }
}
