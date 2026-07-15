using System.Text;
using System.Text.Json;
using GodForge.Application.Common.Constants;
using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Models.Messages;
using GodForge.Infrastructure.Configuration;
using GodForge.Worker.Handlers;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace GodForge.Worker.Queues;

public sealed class RabbitMqWorkerService : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly RabbitMqSettings _settings;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RabbitMqWorkerService> _logger;
    private IConnection? _connection;
    private IModel? _channel;
    private CancellationToken _stoppingToken;

    public RabbitMqWorkerService(
        IOptions<RabbitMqSettings> settings,
        IServiceScopeFactory scopeFactory,
        ILogger<RabbitMqWorkerService> logger)
    {
        _settings = settings.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _stoppingToken = stoppingToken;
        if (!_settings.Enabled)
        {
            _logger.LogWarning("RabbitMQ worker is disabled by configuration.");
            await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
            return;
        }

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
        _channel = _connection.CreateModel();
        _channel.QueueDeclare(WorkerQueueNames.DeadLetter, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueDeclare(
            WorkerQueueNames.RepositoryPipeline,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object>
            {
                ["x-dead-letter-exchange"] = string.Empty,
                ["x-dead-letter-routing-key"] = WorkerQueueNames.DeadLetter
            });
        _channel.BasicQos(0, 1, global: false);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += HandleMessageAsync;
        _channel.BasicConsume(
            queue: WorkerQueueNames.RepositoryPipeline,
            autoAck: false,
            consumer: consumer);

        _logger.LogInformation("Worker is consuming queue {QueueName}", WorkerQueueNames.RepositoryPipeline);
        await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
    }

    private async Task HandleMessageAsync(object sender, BasicDeliverEventArgs eventArgs)
    {
        if (_channel is null)
        {
            return;
        }

        RepositoryAnalysisJobMessage? message;
        try
        {
            var json = Encoding.UTF8.GetString(eventArgs.Body.Span);
            message = JsonSerializer.Deserialize<RepositoryAnalysisJobMessage>(json, JsonOptions);
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(exception, "Invalid worker message JSON was dead-lettered");
            _channel.BasicReject(eventArgs.DeliveryTag, requeue: false);
            return;
        }

        if (message is null || message.SchemaVersion != "1.0")
        {
            _channel.BasicReject(eventArgs.DeliveryTag, requeue: false);
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<RepositoryAnalysisPipelineHandler>();
            var result = await handler.HandleAsync(message, _stoppingToken);

            if (result.Disposition == JobExecutionDisposition.Retry)
            {
                var publisher = scope.ServiceProvider.GetRequiredService<IJobPublisher>();
                await publisher.PublishDelayedAsync(
                    WorkerQueueNames.RepositoryPipeline,
                    message with
                    {
                        MessageId = Guid.NewGuid(),
                        AttemptCount = message.AttemptCount + 1,
                        CreatedAt = DateTimeOffset.UtcNow
                    },
                    result.RetryDelay,
                    _stoppingToken);
                _channel.BasicAck(eventArgs.DeliveryTag, multiple: false);
                return;
            }

            if (result.Disposition == JobExecutionDisposition.DeadLetter)
            {
                _channel.BasicReject(eventArgs.DeliveryTag, requeue: false);
                return;
            }

            _channel.BasicAck(eventArgs.DeliveryTag, multiple: false);
        }
        catch (OperationCanceledException) when (_stoppingToken.IsCancellationRequested)
        {
            _channel.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: true);
        }
        catch (Exception exception)
        {
            if (eventArgs.Redelivered)
            {
                _logger.LogError(exception, "Unhandled worker consumer failure after redelivery; message will be dead-lettered");
                _channel.BasicReject(eventArgs.DeliveryTag, requeue: false);
                return;
            }

            _logger.LogError(exception, "Unhandled worker consumer failure; message will be requeued once");
            _channel.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: true);
        }
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}
