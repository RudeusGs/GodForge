using System.ComponentModel.DataAnnotations;

namespace GodForge.Infrastructure.Configuration;

public sealed class RabbitMqSettings
{
    public const string SectionName = "RabbitMQ";

    public bool Enabled { get; set; }

    public string HostName { get; set; } = string.Empty;

    [Range(1, 65535)]
    public int Port { get; set; } = 5672;

    public string UserName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string VirtualHost { get; set; } = "/";
}
