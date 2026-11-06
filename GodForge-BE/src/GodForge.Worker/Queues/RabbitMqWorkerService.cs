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
    private int _infrastructureFailureCount;

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
        foreach (var queue in new[] { WorkerQueueNames.RepositoryPipeline, WorkerQueueNames.HostedRepositoryProvision, WorkerQueueNames.HostedRepositoryReconcile, WorkerQueueNames.RepositorySync })
        {
            _channel.QueueDeclare(
                queue,
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
            var deliveryChannel = _channel;
            consumer.Received += (sender, args) => HandleMessageAsync(queue, deliveryChannel, sender, args);
            _channel.BasicConsume(
                queue: queue,
                autoAck: false,
                consumer: consumer);
        }
    }

    private static TimeSpan CalculateReconnectDelay(int attempt)
    {
        var exponentialSeconds = Math.Min(30, Math.Pow(2, Math.Min(attempt - 1, 5)));
        return TimeSpan.FromMilliseconds((exponentialSeconds * 1000) + Random.Shared.Next(0, 501));
    }

    private async Task HandleMessageAsync(string queue, IModel deliveryChannel, object sender, BasicDeliverEventArgs eventArgs)
    {
        if (_channel is null)
        {
            return;
        }

        WorkerMessage? message;
        try
        {
            var json = Encoding.UTF8.GetString(eventArgs.Body.Span);
            message = queue switch
            {
                WorkerQueueNames.RepositorySync => JsonSerializer.Deserialize<RepositorySyncJobMessage>(json, JsonOptions),
                WorkerQueueNames.HostedRepositoryProvision => JsonSerializer.Deserialize<HostedRepositoryProvisionJobMessage>(json, JsonOptions),
                WorkerQueueNames.HostedRepositoryReconcile => JsonSerializer.Deserialize<HostedRepositoryReconcileJobMessage>(json, JsonOptions),
                _ => JsonSerializer.Deserialize<RepositoryAnalysisJobMessage>(json, JsonOptions)
            };
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(exception, "Invalid worker message JSON was dead-lettered");
            deliveryChannel.BasicReject(eventArgs.DeliveryTag, requeue: false);
            return;
        }

        if (message is null || message.SchemaVersion != "1.0")
        {
            deliveryChannel.BasicReject(eventArgs.DeliveryTag, requeue: false);
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var result = message switch
            {
                RepositorySyncJobMessage sync => await scope.ServiceProvider.GetRequiredService<RepositorySyncHandler>().HandleAsync(sync, _stoppingToken),
                HostedRepositoryReconcileJobMessage reconcile => await scope.ServiceProvider.GetRequiredService<HostedRepositoryReconcileHandler>().HandleAsync(reconcile, _stoppingToken),
                HostedRepositoryProvisionJobMessage provision => await scope.ServiceProvider.GetRequiredService<HostedRepositoryProvisionHandler>().HandleAsync(provision, _stoppingToken),
                RepositoryAnalysisJobMessage analysis => await scope.ServiceProvider.GetRequiredService<RepositoryAnalysisPipelineHandler>().HandleAsync(analysis, _stoppingToken),
                _ => JobExecutionResult.DeadLetter()
            };

            if (result.Disposition == JobExecutionDisposition.Retry)
            {
                await RecoverAsync(queue, message, deliveryChannel, eventArgs.DeliveryTag);
                return;
            }

            if (result.Disposition == JobExecutionDisposition.DeadLetter)
            {
                deliveryChannel.BasicReject(eventArgs.DeliveryTag, requeue: false);
                return;
            }

            deliveryChannel.BasicAck(eventArgs.DeliveryTag, multiple: false);
            _infrastructureFailureCount = 0;
        }
        catch (OperationCanceledException) when (_stoppingToken.IsCancellationRequested)
        {
            // Closing leaves unacknowledged deliveries recoverable and stops callbacks
            // immediately; repeatedly nacking during shutdown can create a hot loop.
            DisposeConnection();
        }
        catch (Exception exception)
        {
            _logger.LogError("WORKER_DELIVERY_FAILED for job {JobId}; exception type {ExceptionType}", message.JobId, exception.GetType().Name);
            await RecoverAsync(queue, message, deliveryChannel, eventArgs.DeliveryTag);
        }
    }

    private async Task RecoverAsync(string queue, WorkerMessage message, IModel channel, ulong tag)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var result = await scope.ServiceProvider.GetRequiredService<WorkerDeliveryRecovery>().ScheduleAsync(queue, message, _stoppingToken);
            if (result.Disposition == JobExecutionDisposition.DeadLetter) channel.BasicReject(tag, requeue: false);
            else channel.BasicAck(tag, multiple: false);
        }
        catch (OperationCanceledException) when (_stoppingToken.IsCancellationRequested) { DisposeConnection(); }
        catch (Exception)
        {
            // Database/broker outage: no acknowledgement without durable intent. Delay
            // the current delivery using the existing reconnect policy before requeue.
            var delay = CalculateReconnectDelay(Math.Min(10, ++_infrastructureFailureCount));
            _logger.LogWarning("WORKER_RECOVERY_UNAVAILABLE; delaying redelivery by {DelaySeconds} seconds", delay.TotalSeconds);
            try
            {
                await _reconnectDelay.WaitAsync(delay, _stoppingToken);
                if (channel.IsOpen) channel.BasicNack(tag, multiple: false, requeue: true);
            }
            catch (OperationCanceledException) when (_stoppingToken.IsCancellationRequested) { DisposeConnection(); }
            catch (Exception) { DisposeConnection(); }
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
