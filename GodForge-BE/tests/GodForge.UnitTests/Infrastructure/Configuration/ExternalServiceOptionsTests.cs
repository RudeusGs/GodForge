using GodForge.Infrastructure;
using GodForge.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GodForge.UnitTests.Infrastructure.Configuration;

public sealed class ExternalServiceOptionsTests
{
    [Fact]
    public void Forgejo_WhenEnabledWithHttpBaseUrl_FailsOptionsValidation()
    {
        using var provider = BuildProvider(new Dictionary<string, string?>
        {
            ["Forgejo:Enabled"] = "true",
            ["Forgejo:BaseUrl"] = "http://forgejo.example.test",
            ["Forgejo:ApiToken"] = "test-token"
        });

        Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<IOptions<ForgejoSettings>>().Value);
    }

    [Fact]
    public void Gemini_WhenEnabledWithoutApiKey_FailsOptionsValidation()
    {
        using var provider = BuildProvider(new Dictionary<string, string?>
        {
            ["Gemini:Enabled"] = "true",
            ["Gemini:Endpoint"] = "https://generativelanguage.googleapis.com",
            ["Gemini:Model"] = "gemini-test"
        });

        Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<IOptions<GeminiSettings>>().Value);
    }

    [Fact]
    public void Email_WhenPartiallyConfigured_FailsOptionsValidation()
    {
        using var provider = BuildProvider(new Dictionary<string, string?>
        {
            ["Email:Smtp:Host"] = "smtp.example.test"
        });

        Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<IOptions<EmailSettings>>().Value);
    }

    private static ServiceProvider BuildProvider(IReadOnlyDictionary<string, string?> values)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(configuration);
        return services.BuildServiceProvider();
    }
}
