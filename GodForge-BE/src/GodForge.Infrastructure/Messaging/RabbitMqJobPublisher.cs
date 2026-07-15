using System.Text;
using System.Text.Json;
using GodForge.Application.Common.Constants;
using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Models.Messages;
using GodForge.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace GodForge.Infrastructure.Messaging;

public sealed class RabbitMqJobPublisher : IJobPublisher
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly RabbitMqSettings _settings;

    public RabbitMqJobPublisher(IOptions<RabbitMqSettings> settings)
    {
        _settings = settings.Value;
    }

    public Task PublishAsync(
        string queueName,
        WorkerMessage message,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_settings.Enabled)
        {
            throw new InvalidOperationException("RabbitMQ is disabled. Enable RabbitMQ before creating background jobs.");
        }

        var factory = new ConnectionFactory
        {
            HostName = _settings.HostName,
            Port = _settings.Port,
            UserName = _settings.UserName,
            Password = _settings.Password,
            VirtualHost = _settings.VirtualHost,
            DispatchConsumersAsync = true
        };

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();
        DeclareQueue(channel, queueName);

        channel.ConfirmSelect();

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message, message.GetType(), JsonOptions));
        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";
        properties.MessageId = message.MessageId.ToString("N");
        properties.CorrelationId = message.CorrelationId;
        properties.Type = message.GetType().Name;

        channel.BasicPublish(
            exchange: string.Empty,
            routingKey: queueName,
            mandatory: true,
            basicProperties: properties,
            body: body);
        channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(5));

        return Task.CompletedTask;
    }

    private static void DeclareQueue(IModel channel, string queueName)
    {
        channel.QueueDeclare(
            WorkerQueueNames.DeadLetter,
            durable: true,
            exclusive: false,
            autoDelete: false);

        IDictionary<string, object>? arguments = queueName == WorkerQueueNames.DeadLetter
            ? null
            : new Dictionary<string, object>
            {
                ["x-dead-letter-exchange"] = string.Empty,
                ["x-dead-letter-routing-key"] = WorkerQueueNames.DeadLetter
            };

        channel.QueueDeclare(
            queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: arguments);
    }
}

