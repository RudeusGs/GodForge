using System.Text;
using System.Text.Json;
using GodForge.Application.Common.Constants;
using GodForge.Application.Common.Models.Messages;
using GodForge.Infrastructure.Configuration;
using GodForge.Worker.Handlers;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace GodForge.Worker.Queues;

internal sealed class RabbitMqWorkerService : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly RabbitMqSettings _settings;
    private readonly IRabbitMqConnectionFactory _connectionFactory;
    private readonly IRabbitMqReconnectDelay _reconnectDelay;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RabbitMqWorkerService> _logger;
    private IConnection? _connection;
    private IModel? _channel;
    private CancellationToken _stoppingToken;

    public RabbitMqWorkerService(
        IOptions<RabbitMqSettings> settings,
        IRabbitMqConnectionFactory connectionFactory,
        IRabbitMqReconnectDelay reconnectDelay,
        IServiceScopeFactory scopeFactory,
        ILogger<RabbitMqWorkerService> logger)
    {
        _settings = settings.Value;
        _connectionFactory = connectionFactory;
        _reconnectDelay = reconnectDelay;
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

        var attempt = 0;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                ConnectAndConsume();
                _logger.LogInformation("Worker is consuming queue {QueueName}", WorkerQueueNames.RepositoryPipeline);
                await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (BrokerUnreachableException exception)
            {
                DisposeConnection();
                attempt++;
                var delay = CalculateReconnectDelay(attempt);
                _logger.LogWarning(
                    exception,
                    "RabbitMQ is unavailable; connection attempt {Attempt} will be retried in {DelaySeconds} seconds",
                    attempt,
                    delay.TotalSeconds);
                await _reconnectDelay.WaitAsync(delay, stoppingToken);
            }
        }
    }

    private void ConnectAndConsume()
    {
        _connection = _connectionFactory.CreateConnection(_settings);
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
    }

    private static TimeSpan CalculateReconnectDelay(int attempt)
    {
        var exponentialSeconds = Math.Min(30, Math.Pow(2, Math.Min(attempt - 1, 5)));
        return TimeSpan.FromMilliseconds((exponentialSeconds * 1000) + Random.Shared.Next(0, 501));
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
                // Retry scheduling is normally persisted through the database outbox by
                // the handler. Requeue an unexpected Retry result rather than creating a
                // non-transactional RabbitMQ retry message.
                _logger.LogWarning(
                    "Job {JobId} returned an unexpected direct retry disposition; the delivery will be requeued",
                    message.JobId);
                _channel.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: true);
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
            // Consumer-level failures are infrastructure failures. Business/poison
            // messages are converted to DeadLetter by the handler, so an unhandled
            // exception must remain recoverable even after RabbitMQ redelivery.
            _logger.LogError(exception, "Unhandled worker consumer failure; message will be requeued");
            _channel.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: true);
        }
    }

    public override void Dispose()
    {
        DisposeConnection();
        base.Dispose();
    }

    private void DisposeConnection()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        _channel = null;
        _connection = null;
    }
}
