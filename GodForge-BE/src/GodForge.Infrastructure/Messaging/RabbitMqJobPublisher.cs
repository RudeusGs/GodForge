using System.Globalization;
using System.Text;
using System.Text.Json;
using GodForge.Application.Common.Constants;
using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Models.Messages;
using GodForge.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace GodForge.Infrastructure.Messaging;

public sealed class RabbitMqJobPublisher : IJobPublisher, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly RabbitMqSettings _settings;
    private readonly SemaphoreSlim _publishLock = new(1, 1);
    private readonly object _connectionLock = new();
    private IConnection? _connection;
    private bool _disposed;

    public RabbitMqJobPublisher(IOptions<RabbitMqSettings> settings)
    {
        _settings = settings.Value;
    }

    public Task PublishAsync(
        string queueName,
        WorkerMessage message,
        CancellationToken cancellationToken = default)
        => PublishCoreAsync(queueName, message, delay: null, cancellationToken);

    public Task PublishDelayedAsync(
        string queueName,
        WorkerMessage message,
        TimeSpan delay,
        CancellationToken cancellationToken = default)
    {
        if (delay <= TimeSpan.Zero)
        {
            return PublishAsync(queueName, message, cancellationToken);
        }

        return PublishCoreAsync(queueName, message, delay, cancellationToken);
    }

    private async Task PublishCoreAsync(
        string queueName,
        WorkerMessage message,
        TimeSpan? delay,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!_settings.Enabled)
        {
            throw new InvalidOperationException("RabbitMQ is disabled. Enable RabbitMQ before creating background jobs.");
        }

        await _publishLock.WaitAsync(cancellationToken);
        try
        {
            using var channel = GetConnection().CreateModel();
            DeclareQueues(channel, queueName);
            channel.ConfirmSelect();

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message, message.GetType(), JsonOptions));
            var properties = channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.ContentType = "application/json";
            properties.MessageId = message.MessageId.ToString("N");
            properties.CorrelationId = message.CorrelationId;
            properties.Type = message.GetType().Name;

            var routingKey = queueName;
            if (delay is not null)
            {
                routingKey = GetRetryQueueName(queueName);
                var milliseconds = Math.Max(1L, (long)Math.Ceiling(delay.Value.TotalMilliseconds));
                properties.Expiration = milliseconds.ToString(CultureInfo.InvariantCulture);
            }

            channel.BasicPublish(
                exchange: string.Empty,
                routingKey: routingKey,
                mandatory: true,
                basicProperties: properties,
                body: body);
            channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(5));
        }
        catch
        {
            ResetConnectionIfClosed();
            throw;
        }
        finally
        {
            _publishLock.Release();
        }
    }

    private IConnection GetConnection()
    {
        if (_connection is { IsOpen: true })
        {
            return _connection;
        }

        lock (_connectionLock)
        {
            if (_connection is { IsOpen: true })
            {
                return _connection;
            }

            _connection?.Dispose();
            var factory = new ConnectionFactory
            {
                HostName = _settings.HostName,
                Port = _settings.Port,
                UserName = _settings.UserName,
                Password = _settings.Password,
                VirtualHost = _settings.VirtualHost,
                DispatchConsumersAsync = true,
                AutomaticRecoveryEnabled = true
            };
            _connection = factory.CreateConnection();
            return _connection;
        }
    }

    private static void DeclareQueues(IModel channel, string queueName)
    {
        channel.QueueDeclare(
            WorkerQueueNames.DeadLetter,
            durable: true,
            exclusive: false,
            autoDelete: false);

        if (queueName == WorkerQueueNames.DeadLetter)
        {
            return;
        }

        channel.QueueDeclare(
            queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object>
            {
                ["x-dead-letter-exchange"] = string.Empty,
                ["x-dead-letter-routing-key"] = WorkerQueueNames.DeadLetter
            });

        channel.QueueDeclare(
            GetRetryQueueName(queueName),
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object>
            {
                ["x-dead-letter-exchange"] = string.Empty,
                ["x-dead-letter-routing-key"] = queueName
            });
    }

    private static string GetRetryQueueName(string queueName) => $"{queueName}.retry";

    private void ResetConnectionIfClosed()
    {
        lock (_connectionLock)
        {
            if (_connection is null || _connection.IsOpen)
            {
                return;
            }

            _connection.Dispose();
            _connection = null;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _publishLock.Dispose();
        lock (_connectionLock)
        {
            _connection?.Dispose();
            _connection = null;
        }
    }
}
