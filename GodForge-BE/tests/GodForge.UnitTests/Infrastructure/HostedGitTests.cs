using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GodForge.Application.Common.Models;
using GodForge.Application.Common.Models.Analysis;
using GodForge.Application.Features.Repositories;
using GodForge.Domain.Entities.Repo;
using GodForge.Domain.Enums;
using GodForge.Infrastructure.Configuration;
using GodForge.Infrastructure.HostedGit;
using Microsoft.Extensions.Options;

namespace GodForge.UnitTests.Infrastructure;

public sealed class HostedGitTests
{
    [Fact]
    public void ManualRetryUsesTheCurrentlyAuthorizedRequester()
    {
        var now = DateTimeOffset.UtcNow;
        var actor = Guid.NewGuid();
        var job = GodForge.Domain.Entities.Ops.Job.Create(Guid.NewGuid(), Guid.NewGuid(), JobType.ProvisionHostedRepository,
            "repository.hosted.provision", 0, null, "stable-operation", 3, Guid.NewGuid(), "retry", now, now);
        var id = job.Id;
        job.MarkRunning(now);
        job.MarkFailed("FORGEJO_UNAVAILABLE", "Unavailable", now);
        job.RetryHostedProvision(now, actor);
        Assert.Equal(actor, job.TriggeredBy);
        Assert.Equal(id, job.Id);
        Assert.Equal("stable-operation", job.IdempotencyKey);
    }

    [Theory]
    [InlineData("https://example.test/game.git?token=credential")]
    [InlineData("https://example.test/game.git#credential")]
    public void LinkedUrlsCannotPersistQueryOrFragmentCredentials(string remote)
    {
        var command = new GodForge.Application.Features.Repositories.Commands.LinkRepository.LinkRepositoryCommand(
            Guid.NewGuid(), remote, "Generic", "main", null, false, Guid.NewGuid(), "validation");
        Assert.False(new GodForge.Application.Features.Repositories.Commands.LinkRepository.LinkRepositoryCommandValidator().Validate(command).IsValid);
    }

    private static ForgejoSettings Settings() => new() { Enabled = true, BaseUrl = "http://forgejo:3000", PublicUrl = "http://localhost:3000", AllowLocalHttp = true, ApiToken = "server-only", WebhookSecret = new string('s', 32) };
    [Theory]
    [InlineData("../secret")]
    [InlineData("%2e%2e/secret")]
    [InlineData("%252e%252e/secret")]
    [InlineData("a\\b")]
    [InlineData("/etc/passwd")]
    [InlineData("a//b")]
    [InlineData("a/./b")]
    [InlineData("a\u0000b")]
    [InlineData("C:/secret")]
    public async Task UnsafePathNeverCallsProvider(string path)
    {
        var handler = new Stub(_ => throw new Exception("Network access must not occur."));
        var service = new ForgejoHostedGitService(new HttpClient(handler), Options.Create(Settings()));
        var error = await Assert.ThrowsAsync<HostedGitException>(() => service.BlobAsync("1", new string('a', 40), path, default));
        Assert.Equal("REPOSITORY_PATH_INVALID", error.Code);
        Assert.Equal(0, handler.Calls);
    }

    [Theory]
    [InlineData("main", true)]
    [InlineData("feature/godot", true)]
    [InlineData("../main", false)]
    [InlineData("main.lock", false)]
    [InlineData("-main", false)]
    [InlineData("main@{1}", false)]
    [InlineData("main%2fother", false)]
    public void GitBranchBounds(string branch, bool expected) => Assert.Equal(expected, RepositoryInput.ValidBranch(branch));

    [Theory]
    [InlineData(ProjectRole.ProjectOwner, "admin")]
    [InlineData(ProjectRole.Maintainer, "write")]
    [InlineData(ProjectRole.Developer, "write")]
    [InlineData(ProjectRole.Reviewer, "read")]
    [InlineData(ProjectRole.Viewer, "read")]
    public void ProviderRoleIsLeastPrivilege(ProjectRole role, string permission) => Assert.Equal(permission, HostedPermissionMapping.For(role));

    [Fact]
    public void SignatureUsesExactBodyAndRejectsMissingMalformedAndWrongSecret()
    {
        var settings = Settings();
        var verifier = new ForgejoWebhookSignature(Options.Create(settings));
        var body = Encoding.UTF8.GetBytes(" {\"repository\":{\"id\":1}}\n");
        var signature = Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(settings.WebhookSecret), body));
        Assert.True(verifier.Verify(body, signature));
        Assert.False(verifier.Verify(body[..^1], signature));
        Assert.False(verifier.Verify(body, ""));
        Assert.False(verifier.Verify(body, new string('z', 64)));
        settings.WebhookSecret = new string('x', 32);
        Assert.False(verifier.Verify(body, signature));
    }

    [Fact]
    public void PendingRepositoryDoesNotInventProviderIdentity()
    {
        var repo = GitRepository.RequestHosted(Guid.NewGuid(), "godot", "main", false, DateTimeOffset.UtcNow);
        Assert.Null(repo.HostedRepositoryId);
        Assert.Empty(repo.CloneUrlSanitized);
        Assert.Equal(GitRepositoryStatus.ProvisioningPending, repo.GitRepositoryStatus);
        Assert.Throws<InvalidOperationException>(() => repo.CompleteProvisioning("1", "https://forgejo.example/a/b.git", DateTimeOffset.UtcNow));
        repo.BeginProvisioning(DateTimeOffset.UtcNow);
        repo.CompleteProvisioning("1", "https://forgejo.example/a/b.git", DateTimeOffset.UtcNow);
        Assert.Equal(GitRepositoryStatus.Ready, repo.GitRepositoryStatus);
    }

    [Fact]
    public async Task TimeoutRecoveryFindsSameOwnedRepositoryWithoutSecondCreation()
    {
        var marker = Guid.NewGuid().ToString("N");
        var handler = new Stub(request => request.RequestUri!.AbsolutePath == "/api/v1/user"
            ? Json(new { id = 2, login = "service", is_admin = false })
            : Json(new { id = 7, description = $"GodForge managed repository: {marker}", @private = true, owner = new { id = 2 } }));
        var provider = new ForgejoHostedGitService(new HttpClient(handler), Options.Create(Settings()));
        var result = await provider.ProvisionAsync(new("", "godot-123", true, "main", marker));
        Assert.Equal("7", result.ProviderRepositoryId);
        Assert.Equal("http://localhost:3000/service/godot-123.git", result.CloneUrl);
        Assert.DoesNotContain(handler.Methods, method => method == HttpMethod.Post);
    }

    [Fact]
    public async Task NameCollisionCannotAdoptUnrelatedRepository()
    {
        var handler = new Stub(request => request.RequestUri!.AbsolutePath == "/api/v1/user"
            ? Json(new { id = 2, login = "service", is_admin = false })
            : Json(new { id = 7, description = "unrelated", @private = true, owner = new { id = 2 } }));
        var provider = new ForgejoHostedGitService(new HttpClient(handler), Options.Create(Settings()));
        var error = await Assert.ThrowsAsync<HostedGitException>(() => provider.ProvisionAsync(new("", "godot-123", true, "main", Guid.NewGuid().ToString("N"))));
        Assert.Equal("FORGEJO_IDENTITY_CONFLICT", error.Code);
    }

    [Fact]
    public async Task OversizedChunkedProviderBodyIsBounded()
    {
        var handler = new Stub(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(new MemoryStream(new byte[4 * 1024 * 1024 + 1])) });
        var provider = new ForgejoHostedGitService(new HttpClient(handler), Options.Create(Settings()));
        var error = await Assert.ThrowsAsync<HostedGitException>(() => provider.BranchesAsync("1", 1, 20, default));
        Assert.Equal("FORGEJO_RESPONSE_TOO_LARGE", error.Code);
    }

    [Theory]
    [InlineData("http://169.254.169.254", false)]
    [InlineData("http://example.com", false)]
    [InlineData("http://forgejo:3000", true)]
    [InlineData("https://user:secret@example.com", false)]
    [InlineData("https://example.com/?token=secret", false)]
    public void DevelopmentTransportIsNarrow(string endpoint, bool expected)
    {
        var settings = Settings(); settings.BaseUrl = endpoint;
        Assert.Equal(expected, settings.HasSafeEndpoints);
        settings.AllowLocalHttp = false;
        Assert.False(settings.HasSafeEndpoints);
    }

    private static HttpResponseMessage Json(object value) => new(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json") };
    private sealed class Stub(Func<HttpRequestMessage, HttpResponseMessage> response) : HttpMessageHandler
    {
        public int Calls { get; private set; }
        public List<HttpMethod> Methods { get; } = [];
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        { Calls++; Methods.Add(request.Method); return Task.FromResult(response(request)); }
    }
}
