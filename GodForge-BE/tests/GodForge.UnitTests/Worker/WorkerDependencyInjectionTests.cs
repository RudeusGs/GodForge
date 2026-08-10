using GodForge.Infrastructure;
using GodForge.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GodForge.UnitTests.Worker;

public sealed class WorkerDependencyInjectionTests
{
    [Fact]
    public void WorkerCompositionRoot_BuildsWithoutHttpOrIdentityServices()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] =
                    "Host=localhost;Database=godforge_test;Username=godforge;Password=test-only",
                ["OutboxEncryption:Key"] =
                    "worker-composition-test-outbox-key-00000000000000000000"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddInfrastructure(configuration);
        services.AddWorkerServices();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

        Assert.NotNull(provider);
    }
}
