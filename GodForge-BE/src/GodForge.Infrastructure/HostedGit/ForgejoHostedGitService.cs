using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using GodForge.Application.Common.Interfaces;
using GodForge.Application.Common.Models;
using GodForge.Application.Common.Models.Analysis;
using GodForge.Application.Features.Repositories;
using GodForge.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace GodForge.Infrastructure.HostedGit;

public sealed class ForgejoHostedGitService(HttpClient httpClient, IOptions<ForgejoSettings> settings) : IHostedGitService
{
    private const int MaxResponseBytes = 4 * 1024 * 1024;
    private const int MaxBlobBytes = 512 * 1024;
    private readonly ForgejoSettings _settings = settings.Value;
    public bool IsConfigured => _settings.Enabled && _settings.HasSafeEndpoints && !string.IsNullOrWhiteSpace(_settings.ApiToken);

    public async Task<long> EnsureWebhookAsync(string providerId, long? managedHookId, CancellationToken cancellationToken)
    {
        if (!_settings.HasSafeWebhook || _settings.WebhookSecret.Length < 32) throw new HostedGitException("FORGEJO_WEBHOOK_NOT_CONFIGURED");
        var (route, _) = await ResolveAsync(providerId, cancellationToken);
        var candidates = new List<long>();
        for (var page = 1; page <= 20; page++)
        {
            using var hooks = await SendAsync(HttpMethod.Get, $"{route}/hooks?page={page}&limit=100", null, cancellationToken);
            foreach (var hook in hooks.RootElement.EnumerateArray())
                if (hook.GetProperty("id").GetInt64() == managedHookId ||
                    hook.GetProperty("type").GetString() == "forgejo" && hook.GetProperty("config").GetProperty("url").GetString() == _settings.WebhookUrl)
                    candidates.Add(hook.GetProperty("id").GetInt64());
            if (hooks.RootElement.GetArrayLength() < 100) break;
            if (page == 20) throw new HostedGitException("FORGEJO_RESPONSE_TOO_LARGE");
        }
        var body = new
        {
            type = "forgejo",
            active = true,
            events = new[] { "push", "create", "delete", "repository" },
            config = new { url = _settings.WebhookUrl, content_type = "json", secret = _settings.WebhookSecret, http_method = "POST" }
        };
        var id = candidates.FirstOrDefault();
        // Always patch the secret so rotations and uncertain create outcomes converge.
        using var written = await SendAsync(id == 0 ? HttpMethod.Post : HttpMethod.Patch,
            id == 0 ? $"{route}/hooks" : $"{route}/hooks/{id}", body, cancellationToken);
        id = written.RootElement.GetProperty("id").GetInt64();
        using var verified = await SendAsync(HttpMethod.Get, $"{route}/hooks/{id}", null, cancellationToken);
        var value = verified.RootElement;
        if (!value.GetProperty("active").GetBoolean() || value.GetProperty("type").GetString() != "forgejo" ||
            value.GetProperty("config").GetProperty("url").GetString() != _settings.WebhookUrl ||
            value.GetProperty("config").GetProperty("content_type").GetString() != "json" ||
            !value.GetProperty("events").EnumerateArray().Any(e => e.GetString() == "push"))
            throw new HostedGitException("FORGEJO_WEBHOOK_UNCONFIRMED", true);
        foreach (var duplicate in candidates.Where(existing => existing != id))
        { using var removed = await SendAsync(HttpMethod.Delete, $"{route}/hooks/{duplicate}", null, cancellationToken); }
        return id;
    }

    public async Task<ForgejoUserDto> VerifyIdentityAsync(string personalToken, CancellationToken cancellationToken)
    {
        if (personalToken.Length is < 20 or > 1000 || personalToken.Any(char.IsWhiteSpace))
            throw new HostedGitException("FORGEJO_IDENTITY_INVALID");
        using var identity = await SendAsync(HttpMethod.Get, "api/v1/user", null, cancellationToken, personalToken);
        using var service = await SendAsync(HttpMethod.Get, "api/v1/user", null, cancellationToken);
        if (identity.RootElement.GetProperty("is_admin").GetBoolean() || identity.RootElement.GetProperty("id").GetInt64() == service.RootElement.GetProperty("id").GetInt64())
            throw new HostedGitException("FORGEJO_IDENTITY_INVALID");
        return new(identity.RootElement.GetProperty("id").GetInt64(), identity.RootElement.GetProperty("login").GetString()!);
    }

    public async Task<IReadOnlyList<ForgejoCollaboratorDto>> CollaboratorsAsync(string providerId, CancellationToken cancellationToken)
    {
        var (route, _) = await ResolveAsync(providerId, cancellationToken);
        var result = new List<ForgejoCollaboratorDto>();
        for (var page = 1; page <= 100; page++)
        {
            using var doc = await SendAsync(HttpMethod.Get, $"{route}/collaborators?page={page}&limit=100", null, cancellationToken);
            foreach (var user in doc.RootElement.EnumerateArray())
            {
                var login = user.GetProperty("login").GetString()!;
                using var permission = await SendAsync(HttpMethod.Get, $"{route}/collaborators/{E(login)}/permission", null, cancellationToken);
                var effective = permission.RootElement.GetProperty("permission").GetString();
                if (effective is not ("read" or "write" or "admin")) throw new HostedGitException("FORGEJO_RESPONSE_INVALID");
                result.Add(new(user.GetProperty("id").GetInt64(), login, effective));
            }
            if (doc.RootElement.GetArrayLength() < 100) return result;
        }
        throw new HostedGitException("FORGEJO_RESPONSE_TOO_LARGE");
    }

    public async Task SetCollaboratorAsync(string providerId, long userId, string login, string? permission, CancellationToken cancellationToken)
    {
        var (route, _) = await ResolveAsync(providerId, cancellationToken);
        if (permission is not null)
        {
            // A renamed or recycled login is never sufficient evidence for a grant.
            using var user = await SendAsync(HttpMethod.Get, $"api/v1/users/{E(login)}", null, cancellationToken);
            if (user.RootElement.GetProperty("id").GetInt64() != userId) throw new HostedGitException("FORGEJO_IDENTITY_CONFLICT");
        }
        using var result = await SendAsync(permission is null ? HttpMethod.Delete : HttpMethod.Put,
            $"{route}/collaborators/{E(login)}", permission is null ? null : new { permission }, cancellationToken);
    }

    public async Task<HostedRepositoryProvisionResult> ProvisionAsync(HostedRepositoryProvisionRequest request, CancellationToken cancellationToken = default)
    {
        if (!request.IsPrivate || request.ProvisionIdentity is not { Length: 32 } || !RepositoryInput.ValidBranch(request.DefaultBranch))
            throw new HostedGitException("REPOSITORY_PROVISION_FAILED");
        using var user = await SendAsync(HttpMethod.Get, "api/v1/user", null, cancellationToken);
        if (user.RootElement.GetProperty("is_admin").GetBoolean()) throw new HostedGitException("FORGEJO_SERVICE_ACCOUNT_INVALID");
        var owner = user.RootElement.GetProperty("login").GetString()!;
        var route = $"api/v1/repos/{E(owner)}/{E(request.Name)}";
        var marker = $"GodForge managed repository: {request.ProvisionIdentity}";
        JsonDocument? result = await SendOptionalAsync(route, cancellationToken);
        if (result is null)
        {
            try
            {
                result = await SendAsync(HttpMethod.Post, "api/v1/user/repos", new
                {
                    name = request.Name,
                    description = marker,
                    @private = true,
                    default_branch = request.DefaultBranch,
                    auto_init = false
                }, cancellationToken);
            }
            catch (HostedGitException exception) when (exception.Code == "REPOSITORY_ALREADY_CONNECTED")
            {
                result = await SendAsync(HttpMethod.Get, route, null, cancellationToken);
            }
        }
        using (result)
        {
            var repo = result.RootElement;
            if (repo.GetProperty("description").GetString() != marker || !repo.GetProperty("private").GetBoolean() ||
                repo.GetProperty("owner").GetProperty("id").GetInt64() != user.RootElement.GetProperty("id").GetInt64())
                throw new HostedGitException("FORGEJO_IDENTITY_CONFLICT");
            var id = repo.GetProperty("id").GetInt64();
            if (id <= 0) throw new HostedGitException("FORGEJO_RESPONSE_INVALID");
            var web = new Uri(new Uri(_settings.PublicUrl.TrimEnd('/') + "/"), $"{E(owner)}/{E(request.Name)}").AbsoluteUri;
            return new(id.ToString(CultureInfo.InvariantCulture), web + ".git", web, request.DefaultBranch);
        }
    }

    public async Task<IReadOnlyList<RepositoryBranchDto>> BranchesAsync(string providerId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var (route, defaultBranch) = await ResolveAsync(providerId, cancellationToken);
        using var doc = await SendAsync(HttpMethod.Get, $"{route}/branches?page={page}&limit={pageSize}", null, cancellationToken);
        if (doc.RootElement.ValueKind == JsonValueKind.Null) return [];
        return doc.RootElement.EnumerateArray().Select(b => new RepositoryBranchDto(
            b.GetProperty("name").GetString()!, b.GetProperty("commit").GetProperty("id").GetString()!,
            b.GetProperty("name").GetString() == defaultBranch, b.TryGetProperty("protected", out var p) && p.GetBoolean())).ToArray();
    }

    public async Task<IReadOnlyList<RepositoryCommitDto>> CommitsAsync(string providerId, string branch, int page, int pageSize, CancellationToken cancellationToken)
    {
        if (!RepositoryInput.ValidBranch(branch)) throw new HostedGitException("REPOSITORY_REF_INVALID");
        var (route, _) = await ResolveAsync(providerId, cancellationToken);
        using var doc = await SendAsync(HttpMethod.Get, $"{route}/commits?sha={E(branch)}&page={page}&limit={pageSize}", null, cancellationToken);
        if (doc.RootElement.ValueKind == JsonValueKind.Null) return [];
        return doc.RootElement.EnumerateArray().Select(c =>
        {
            var sha = c.GetProperty("sha").GetString()!;
            var commit = c.GetProperty("commit");
            var author = commit.GetProperty("author");
            return new RepositoryCommitDto(sha, sha[..Math.Min(8, sha.Length)], commit.GetProperty("message").GetString()!,
                author.GetProperty("name").GetString()!, author.GetProperty("date").GetDateTimeOffset(),
                c.GetProperty("parents").EnumerateArray().Select(p => p.GetProperty("sha").GetString()!).ToArray(),
                commit.GetProperty("tree").GetProperty("sha").GetString()!, author.GetProperty("email").GetString()!);
        }).ToArray();
    }

    public async Task<RepositoryTreeDto> TreeAsync(string providerId, string commitSha, string path, CancellationToken cancellationToken)
    {
        ValidateTreeInput(commitSha, path, true);
        var (route, _) = await ResolveAsync(providerId, cancellationToken);
        var treeSha = commitSha;
        if (path.Length > 0)
        {
            foreach (var segment in path.Split('/'))
            {
                using var parent = await ReadTreeAsync(route, treeSha, cancellationToken);
                var entry = parent.RootElement.GetProperty("tree").EnumerateArray().FirstOrDefault(e => e.GetProperty("path").GetString() == segment);
                if (entry.ValueKind == JsonValueKind.Undefined || entry.GetProperty("type").GetString() != "tree")
                    throw new HostedGitException("REPOSITORY_PATH_INVALID");
                treeSha = entry.GetProperty("sha").GetString()!;
            }
        }
        using var doc = await ReadTreeAsync(route, treeSha, cancellationToken);
        return new(commitSha, doc.RootElement.GetProperty("tree").EnumerateArray().Select(e =>
        {
            var name = e.GetProperty("path").GetString()!;
            var mode = e.GetProperty("mode").GetString();
            var type = mode is "120000" or "160000" ? "unsupported" : e.GetProperty("type").GetString()!;
            return new RepositoryTreeEntryDto(name, path.Length == 0 ? name : path + "/" + name, type,
                e.TryGetProperty("size", out var size) ? size.GetInt64() : 0, e.GetProperty("sha").GetString()!);
        }).ToArray());
    }

    public async Task<RepositoryBlobDto> BlobAsync(string providerId, string commitSha, string path, CancellationToken cancellationToken)
    {
        ValidateTreeInput(commitSha, path, false);
        var slash = path.LastIndexOf('/');
        var tree = await TreeAsync(providerId, commitSha, slash < 0 ? "" : path[..slash], cancellationToken);
        var entry = tree.Entries.FirstOrDefault(e => e.Path == path);
        if (entry is null || entry.Type != "blob") throw new HostedGitException("REPOSITORY_PATH_INVALID");
        if (entry.Size > MaxBlobBytes) throw new HostedGitException("REPOSITORY_BLOB_TOO_LARGE");
        var (route, _) = await ResolveAsync(providerId, cancellationToken);
        using var doc = await SendAsync(HttpMethod.Get, $"{route}/git/blobs/{E(entry.Sha)}", null, cancellationToken);
        var root = doc.RootElement;
        if (root.GetProperty("size").GetInt64() > MaxBlobBytes || root.GetProperty("encoding").GetString() != "base64")
            throw new HostedGitException("REPOSITORY_BLOB_TOO_LARGE");
        byte[] bytes;
        try { bytes = Convert.FromBase64String(root.GetProperty("content").GetString()!); }
        catch (FormatException) { throw new HostedGitException("FORGEJO_RESPONSE_INVALID"); }
        if (bytes.Length > MaxBlobBytes) throw new HostedGitException("REPOSITORY_BLOB_TOO_LARGE");
        if (bytes.Any(b => b == 0 || b < 9 || b is > 13 and < 32)) throw new HostedGitException("REPOSITORY_BINARY_FILE");
        try { return new(commitSha, path, new UTF8Encoding(false, true).GetString(bytes), bytes.Length); }
        catch (DecoderFallbackException) { throw new HostedGitException("REPOSITORY_BINARY_FILE"); }
    }

    private async Task<JsonDocument> ReadTreeAsync(string route, string sha, CancellationToken cancellationToken)
    {
        var doc = await SendAsync(HttpMethod.Get, $"{route}/git/trees/{E(sha)}?recursive=false&per_page=1001", null, cancellationToken);
        if (doc.RootElement.GetProperty("tree").GetArrayLength() > 1000 || doc.RootElement.TryGetProperty("truncated", out var t) && t.GetBoolean())
        {
            doc.Dispose();
            throw new HostedGitException("REPOSITORY_TREE_TOO_LARGE");
        }
        return doc;
    }

    private async Task<(string Route, string DefaultBranch)> ResolveAsync(string providerId, CancellationToken cancellationToken)
    {
        if (!long.TryParse(providerId, NumberStyles.None, CultureInfo.InvariantCulture, out var id) || id <= 0)
            throw new HostedGitException("REPOSITORY_NOT_CONNECTED");
        using var doc = await SendAsync(HttpMethod.Get, $"api/v1/repositories/{id}", null, cancellationToken);
        var root = doc.RootElement;
        if (root.GetProperty("id").GetInt64() != id) throw new HostedGitException("FORGEJO_IDENTITY_CONFLICT");
        return ($"api/v1/repos/{E(root.GetProperty("owner").GetProperty("login").GetString()!)}/{E(root.GetProperty("name").GetString()!)}",
            root.GetProperty("default_branch").GetString()!);
    }

    private static void ValidateTreeInput(string sha, string path, bool allowEmpty)
    {
        if (!RepositoryInput.ValidSha(sha)) throw new HostedGitException("REPOSITORY_REF_INVALID");
        if (!RepositoryInput.ValidPath(path, allowEmpty)) throw new HostedGitException("REPOSITORY_PATH_INVALID");
    }

    private async Task<JsonDocument?> SendOptionalAsync(string route, CancellationToken cancellationToken)
    {
        try { return await SendAsync(HttpMethod.Get, route, null, cancellationToken); }
        catch (HostedGitException e) when (e.Code == "REPOSITORY_PROVIDER_NOT_FOUND") { return null; }
    }

    private async Task<JsonDocument> SendAsync(HttpMethod method, string route, object? body, CancellationToken cancellationToken, string? personalToken = null)
    {
        if (!IsConfigured) throw new HostedGitException("FORGEJO_NOT_CONFIGURED");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(_settings.TimeoutSeconds));
        using var message = new HttpRequestMessage(method, new Uri(new Uri(_settings.BaseUrl.TrimEnd('/') + "/"), route));
        message.Headers.Authorization = new AuthenticationHeaderValue("token", personalToken ?? _settings.ApiToken);
        if (body is not null) message.Content = JsonContent.Create(body);
        try
        {
            using var response = await httpClient.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            if (method == HttpMethod.Get && route.Contains("/commits?", StringComparison.Ordinal) && response.StatusCode == HttpStatusCode.Conflict)
                return JsonDocument.Parse("[]");
            if (response.StatusCode == HttpStatusCode.NoContent) return JsonDocument.Parse("{}");
            if (!response.IsSuccessStatusCode)
                throw new HostedGitException(response.StatusCode switch
                {
                    HttpStatusCode.NotFound => "REPOSITORY_PROVIDER_NOT_FOUND",
                    HttpStatusCode.Conflict => "REPOSITORY_ALREADY_CONNECTED",
                    HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => "REPOSITORY_PERMISSION_DENIED",
                    _ => "FORGEJO_UNAVAILABLE"
                }, (int)response.StatusCode >= 500 || response.StatusCode == HttpStatusCode.TooManyRequests);
            if (response.Content.Headers.ContentLength > MaxResponseBytes) throw new HostedGitException("FORGEJO_RESPONSE_TOO_LARGE");
            await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
            using var buffer = new MemoryStream();
            var chunk = new byte[8192];
            int read;
            while ((read = await stream.ReadAsync(chunk, timeout.Token)) > 0)
            {
                if (buffer.Length + read > MaxResponseBytes) throw new HostedGitException("FORGEJO_RESPONSE_TOO_LARGE");
                buffer.Write(chunk, 0, read);
            }
            return JsonDocument.Parse(buffer.ToArray(), new JsonDocumentOptions { MaxDepth = 32 });
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { throw new HostedGitException("FORGEJO_UNAVAILABLE", true); }
        catch (HttpRequestException) { throw new HostedGitException("FORGEJO_UNAVAILABLE", true); }
        catch (JsonException) { throw new HostedGitException("FORGEJO_RESPONSE_INVALID"); }
    }

    private static string E(string value) => Uri.EscapeDataString(value);

    public async Task<bool> ProtectionAsync(string providerId, string branch, bool? enabled, CancellationToken cancellationToken)
    {
        if (!RepositoryInput.ValidBranch(branch)) throw new HostedGitException("REPOSITORY_REF_INVALID");
        var (route, _) = await ResolveAsync(providerId, cancellationToken);
        var rule = $"{route}/branch_protections/{E(branch)}";
        using var current = await SendOptionalAsync(rule, cancellationToken);
        if (enabled is null) return current is not null;
        if (enabled.Value)
        {
            using var write = await SendAsync(current is null ? HttpMethod.Post : HttpMethod.Patch,
                current is null ? $"{route}/branch_protections" : rule,
                new { rule_name = branch, enable_push = true, enable_push_whitelist = false, apply_to_admins = true }, cancellationToken);
        }
        else if (current is not null) { using var deleted = await SendAsync(HttpMethod.Delete, rule, null, cancellationToken); }
        using var verified = await SendOptionalAsync(rule, cancellationToken);
        if ((verified is not null) != enabled.Value) throw new HostedGitException("FORGEJO_RECONCILIATION_INCOMPLETE", true);
        if (verified is not null && !verified.RootElement.GetProperty("enable_push").GetBoolean())
            throw new HostedGitException("FORGEJO_RECONCILIATION_INCOMPLETE");
        return enabled.Value;
    }
}
