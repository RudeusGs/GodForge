using GodForge.Infrastructure;
using GodForge.Worker.Handlers;
using GodForge.Worker.Handlers.Stages;
using GodForge.Worker.Queues;
using Microsoft.Extensions.DependencyInjection;

namespace GodForge.Worker;

public static class DependencyInjection
{
    public static IServiceCollection AddWorkerServices(this IServiceCollection services)
    {
        services.AddOutboxDispatching();
        services.AddSingleton<IRabbitMqConnectionFactory, RabbitMqConnectionFactory>();
        services.AddSingleton<IRabbitMqReconnectDelay, RabbitMqReconnectDelay>();
        services.AddScoped<RepositoryDeterministicAnalysisStage>();
        services.AddScoped<RepositoryAnalysisPersistenceStage>();
        services.AddScoped<RepositoryAiAnalysisStage>();
        services.AddScoped<RepositoryAnalysisPipelineHandler>();
        services.AddHostedService<RabbitMqWorkerService>();

        return services;
    }
}
