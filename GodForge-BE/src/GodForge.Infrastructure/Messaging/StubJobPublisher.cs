using GodForge.Application.Common.Interfaces;
using GodForge.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace GodForge.Infrastructure.Messaging;

public sealed class StubJobPublisher : IJobPublisher
{
    private readonly ILogger<StubJobPublisher> _logger;

    public StubJobPublisher(ILogger<StubJobPublisher> logger)
    {
        _logger = logger;
    }

    public Task PublishAsync(Guid jobId, Guid projectId, JobType type, string correlationId, string? inputHash, CancellationToken cancellationToken = default)
    {
        // This is a stub for development. In production, this would publish to RabbitMQ.
        _logger.LogInformation("Job published to stub queue: {JobId}, Type: {JobType}, Project: {ProjectId}, CorrelationId: {CorrelationId}", 
            jobId, type, projectId, correlationId);
            
        return Task.CompletedTask;
    }
}
