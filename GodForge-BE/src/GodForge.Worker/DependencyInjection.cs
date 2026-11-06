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
        services.AddScoped<GodForge.Application.Common.Security.IAuthorizationService, GodForge.Application.Common.Security.AuthorizationService>();
        services.AddSingleton<IRabbitMqConnectionFactory, RabbitMqConnectionFactory>();
        services.AddSingleton<IRabbitMqReconnectDelay, RabbitMqReconnectDelay>();
        services.AddScoped<RepositoryDeterministicAnalysisStage>();
        services.AddScoped<RepositoryAnalysisPersistenceStage>();
        services.AddScoped<RepositoryAiAnalysisStage>();
        services.AddScoped<RepositoryAnalysisPipelineHandler>();
        services.AddScoped<HostedRepositoryProvisionHandler>();
        services.AddScoped<M2JobExecution>();
        services.AddScoped<WorkerDeliveryRecovery>();
        services.AddScoped<HostedRepositoryReconcileHandler>();
        services.AddScoped<RepositorySyncHandler>();
        services.AddHostedService<GodForge.Infrastructure.HostedGit.HostedPermissionDriftService>();
        services.AddHostedService<RabbitMqWorkerService>();

        return services;
    }
}
