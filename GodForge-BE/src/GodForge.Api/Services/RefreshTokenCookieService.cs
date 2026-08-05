namespace GodForge.Api.Services;

public sealed class RefreshTokenCookieService
{
    private const string CookieName = "godforge_refresh";
    private const string CookiePath = "/api/v1/auth";
    private readonly IHostEnvironment _environment;

    public RefreshTokenCookieService(IHostEnvironment environment)
        => _environment = environment;

    public string? Read(HttpRequest request)
        => request.Cookies.TryGetValue(CookieName, out var token) && !string.IsNullOrWhiteSpace(token)
            ? token
            : null;

    public void Write(HttpResponse response, string token, DateTimeOffset expiresAt)
    {
        response.Cookies.Append(CookieName, token, CreateOptions(expiresAt));
    }

    public void Delete(HttpResponse response)
    {
        response.Cookies.Delete(CookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = !_environment.IsDevelopment(),
            SameSite = SameSiteMode.Strict,
            Path = CookiePath,
            IsEssential = true
        });
    }

    private CookieOptions CreateOptions(DateTimeOffset expiresAt) => new()
    {
        HttpOnly = true,
        Secure = !_environment.IsDevelopment(),
        SameSite = SameSiteMode.Strict,
        Path = CookiePath,
        Expires = expiresAt,
        IsEssential = true
    };
}
