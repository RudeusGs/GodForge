using GodForge.Infrastructure.Configuration;
using RabbitMQ.Client;

namespace GodForge.Worker.Queues;

internal interface IRabbitMqConnectionFactory
{
    IConnection CreateConnection(RabbitMqSettings settings);
}

internal sealed class RabbitMqConnectionFactory : IRabbitMqConnectionFactory
{
    public IConnection CreateConnection(RabbitMqSettings settings)
    {
        var factory = new ConnectionFactory
        {
            HostName = settings.HostName,
            Port = settings.Port,
            UserName = settings.UserName,
            Password = settings.Password,
            VirtualHost = settings.VirtualHost,
            DispatchConsumersAsync = true,
            AutomaticRecoveryEnabled = true
        };

        return factory.CreateConnection();
    }
}

internal interface IRabbitMqReconnectDelay
{
    Task WaitAsync(TimeSpan delay, CancellationToken cancellationToken);
}

internal sealed class RabbitMqReconnectDelay : IRabbitMqReconnectDelay
{
    public Task WaitAsync(TimeSpan delay, CancellationToken cancellationToken)
        => Task.Delay(delay, cancellationToken);
}
