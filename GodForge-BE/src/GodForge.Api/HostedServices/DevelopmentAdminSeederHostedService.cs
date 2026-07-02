using GodForge.Application.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GodForge.Api.HostedServices;

public sealed class DevelopmentAdminSeederHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;

    public DevelopmentAdminSeederHostedService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<IAdminSeeder>();
        await seeder.SeedAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
