using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace GodForge.Infrastructure.Configuration;

public sealed class EmailSettings
{
    [ValidateObjectMembers]
    public SmtpSettings Smtp { get; set; } = new();
}

public sealed class SmtpSettings
{
    [MaxLength(255)]
    public string Host { get; set; } = string.Empty;

    [Range(1, 65535)]
    public int Port { get; set; } = 587;

    public bool EnableSsl { get; set; } = true;

    [MaxLength(320)]
    public string UserName { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Password { get; set; } = string.Empty;

    [MaxLength(320)]
    public string FromEmail { get; set; } = string.Empty;

    [MaxLength(200)]
    public string FromName { get; set; } = string.Empty;

    public bool IsUnconfigured =>
        string.IsNullOrWhiteSpace(Host) &&
        string.IsNullOrWhiteSpace(UserName) &&
        string.IsNullOrWhiteSpace(Password) &&
        string.IsNullOrWhiteSpace(FromEmail) &&
        string.IsNullOrWhiteSpace(FromName);
}
