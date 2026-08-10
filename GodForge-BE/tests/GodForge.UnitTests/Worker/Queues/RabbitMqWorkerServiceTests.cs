using GodForge.Infrastructure.Configuration;
using GodForge.Worker.Queues;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace GodForge.UnitTests.Worker.Queues;

public sealed class RabbitMqWorkerServiceTests
{
    [Fact]
    public async Task StartAsync_WhenBrokerIsUnavailable_RetriesWithoutStoppingHost()
    {
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var connectionFactory = new UnavailableConnectionFactory();
        var reconnectDelay = new RecordingReconnectDelay(cancellation);
        var services = new ServiceCollection().BuildServiceProvider();
        var worker = new RabbitMqWorkerService(
            Options.Create(new RabbitMqSettings { Enabled = true }),
            connectionFactory,
            reconnectDelay,
            services.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<RabbitMqWorkerService>.Instance);

        await worker.StartAsync(cancellation.Token);
        await reconnectDelay.SecondRetryObserved.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await worker.StopAsync(CancellationToken.None);

        Assert.True(connectionFactory.AttemptCount >= 2);
    }

    private sealed class UnavailableConnectionFactory : IRabbitMqConnectionFactory
    {
        public int AttemptCount { get; private set; }

        public IConnection CreateConnection(RabbitMqSettings settings)
        {
            AttemptCount++;
            throw new BrokerUnreachableException(new InvalidOperationException("Broker unavailable in test."));
        }
    }

    private sealed class RecordingReconnectDelay : IRabbitMqReconnectDelay
    {
        private readonly CancellationTokenSource _cancellation;
        private int _waitCount;

        public RecordingReconnectDelay(CancellationTokenSource cancellation)
        {
            _cancellation = cancellation;
        }

        public TaskCompletionSource SecondRetryObserved { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task WaitAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            if (Interlocked.Increment(ref _waitCount) >= 2)
            {
                SecondRetryObserved.TrySetResult();
                _cancellation.Cancel();
            }

            return Task.CompletedTask;
        }
    }
}
