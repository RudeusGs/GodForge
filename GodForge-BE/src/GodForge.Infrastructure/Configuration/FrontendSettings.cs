using System.ComponentModel.DataAnnotations;

namespace GodForge.Infrastructure.Configuration;

public sealed class FrontendSettings
{
    [Required]
    [Url]
    public string BaseUrl { get; set; } = "http://localhost:5173";
}
