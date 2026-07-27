using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Models.Analysis;
using GodForge.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace GodForge.Infrastructure.HostedGit;

public sealed class ForgejoHostedGitService : IHostedGitService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly ForgejoSettings _settings;

    public ForgejoHostedGitService(HttpClient httpClient, IOptions<ForgejoSettings> settings)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
    }

    public async Task<HostedRepositoryProvisionResult> ProvisionAsync(
        HostedRepositoryProvisionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled || string.IsNullOrWhiteSpace(_settings.ApiToken))
        {
            throw new InvalidOperationException("Forgejo hosted Git is not configured.");
        }

        using var message = new HttpRequestMessage(
            HttpMethod.Post,
            string.IsNullOrWhiteSpace(request.Owner)
                ? "api/v1/user/repos"
                : $"api/v1/orgs/{Uri.EscapeDataString(request.Owner)}/repos");

        message.Headers.Authorization = new AuthenticationHeaderValue("token", _settings.ApiToken);
        message.Content = JsonContent.Create(new
        {
            name = request.Name,
            @private = request.IsPrivate,
            default_branch = request.DefaultBranch,
            auto_init = false
        }, options: JsonOptions);

        using var response = await _httpClient.SendAsync(message, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ForgejoRepositoryResponse>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Forgejo returned an empty repository response.");

        return new HostedRepositoryProvisionResult(
            result.Id.ToString(),
            result.CloneUrl,
            result.HtmlUrl,
            result.DefaultBranch ?? request.DefaultBranch);
    }

    private sealed record ForgejoRepositoryResponse(
        long Id,
        [property: JsonPropertyName("clone_url")] string CloneUrl,
        [property: JsonPropertyName("html_url")] string HtmlUrl,
        [property: JsonPropertyName("default_branch")] string? DefaultBranch);
}
