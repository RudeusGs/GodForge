using GodForge.Infrastructure.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GodForge.UnitTests.Infrastructure.Configuration;

public class RabbitMqSettingsValidationTests
{
    private static IOptions<RabbitMqSettings> GetConfiguredOptions(Action<RabbitMqSettings> configure)
    {
        var services = new ServiceCollection();

        services.AddOptions<RabbitMqSettings>()
            .Configure(configure)
            .ValidateDataAnnotations()
            .Validate(
                settings =>
                    !settings.Enabled ||
                    !string.IsNullOrWhiteSpace(settings.HostName),
                "RabbitMQ HostName is required when RabbitMQ is enabled.")
            .Validate(
                settings =>
                    !settings.Enabled ||
                    !string.IsNullOrWhiteSpace(settings.UserName),
                "RabbitMQ UserName is required when RabbitMQ is enabled.")
            .Validate(
                settings =>
                    !settings.Enabled ||
                    !string.IsNullOrWhiteSpace(settings.Password),
                "RabbitMQ Password is required when RabbitMQ is enabled.")
            .Validate(
                settings =>
                    !settings.Enabled ||
                    !string.IsNullOrWhiteSpace(settings.VirtualHost),
                "RabbitMQ VirtualHost is required when RabbitMQ is enabled.")
            .Validate(
                settings =>
                    !settings.Enabled ||
                    !(
                        string.Equals(
                            settings.UserName,
                            "guest",
                            StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(
                            settings.Password,
                            "guest",
                            StringComparison.Ordinal)
                    ),
                "RabbitMQ guest/guest credentials are not allowed.");

        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IOptions<RabbitMqSettings>>();
    }

    [Fact]
    public void Validation_Passes_WhenDisabledAndFieldsEmpty()
    {
        var options = GetConfiguredOptions(s =>
        {
            s.Enabled = false;
            s.HostName = "";
            s.UserName = "";
            s.Password = "";
        });

        // Act & Assert
        var exception = Record.Exception(() => _ = options.Value);
        Assert.Null(exception);
    }

    [Fact]
    public void Validation_Fails_WhenEnabledAndHostNameMissing()
    {
        var options = GetConfiguredOptions(s =>
        {
            s.Enabled = true;
            s.HostName = "";
            s.UserName = "user";
            s.Password = "pass";
        });

        var exception = Assert.Throws<OptionsValidationException>(() => _ = options.Value);
        Assert.Contains("RabbitMQ HostName is required", exception.Message);
    }

    [Fact]
    public void Validation_Fails_WhenEnabledAndUserNameMissing()
    {
        var options = GetConfiguredOptions(s =>
        {
            s.Enabled = true;
            s.HostName = "localhost";
            s.UserName = "";
            s.Password = "pass";
        });

        var exception = Assert.Throws<OptionsValidationException>(() => _ = options.Value);
        Assert.Contains("RabbitMQ UserName is required", exception.Message);
    }

    [Fact]
    public void Validation_Fails_WhenEnabledAndPasswordMissing()
    {
        var options = GetConfiguredOptions(s =>
        {
            s.Enabled = true;
            s.HostName = "localhost";
            s.UserName = "user";
            s.Password = "";
        });

        var exception = Assert.Throws<OptionsValidationException>(() => _ = options.Value);
        Assert.Contains("RabbitMQ Password is required", exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(65536)]
    public void Validation_Fails_WhenPortIsInvalid(int invalidPort)
    {
        var options = GetConfiguredOptions(s =>
        {
            s.Enabled = true;
            s.HostName = "localhost";
            s.UserName = "user";
            s.Password = "pass";
            s.Port = invalidPort;
        });

        var exception = Assert.Throws<OptionsValidationException>(() => _ = options.Value);
        Assert.Contains("Port", exception.Message);
    }

    [Fact]
    public void Validation_Fails_WhenGuestCredentialsUsed()
    {
        var options = GetConfiguredOptions(s =>
        {
            s.Enabled = true;
            s.HostName = "localhost";
            s.UserName = "guest";
            s.Password = "guest";
        });

        var exception = Assert.Throws<OptionsValidationException>(() => _ = options.Value);
        Assert.Contains("RabbitMQ guest/guest credentials are not allowed", exception.Message);
    }

    [Fact]
    public void Validation_Passes_WhenValidLocalConfiguration()
    {
        var options = GetConfiguredOptions(s =>
        {
            s.Enabled = true;
            s.HostName = "localhost";
            s.UserName = "godforge";
            s.Password = "godforge_local_password";
            s.Port = 5672;
            s.VirtualHost = "/";
        });

        var exception = Record.Exception(() => _ = options.Value);
        Assert.Null(exception);
    }
}
