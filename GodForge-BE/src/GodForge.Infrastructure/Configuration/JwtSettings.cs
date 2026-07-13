using System.ComponentModel.DataAnnotations;

namespace GodForge.Infrastructure.Configuration;

public class JwtSettings
{
    [Required(ErrorMessage = "JWT Secret is required.")]
    [MinLength(32, ErrorMessage = "A secure Jwt:Secret of at least 32 characters is required.")]
    public string Secret { get; set; } = string.Empty;

    [Required(ErrorMessage = "JWT Issuer is required.")]
    public string Issuer { get; set; } = string.Empty;

    [Required(ErrorMessage = "JWT Audience is required.")]
    public string Audience { get; set; } = string.Empty;

    [Range(1, 100000, ErrorMessage = "JWT ExpiryMinutes must be greater than 0.")]
    public int ExpiryMinutes { get; set; } = 15;
}
