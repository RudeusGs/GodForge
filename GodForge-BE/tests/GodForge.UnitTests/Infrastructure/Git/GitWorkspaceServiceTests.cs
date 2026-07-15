using System.Net.Sockets;
using GodForge.Infrastructure.Configuration;
using GodForge.Infrastructure.Git;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace GodForge.UnitTests.Infrastructure.Git;

public class GitWorkspaceServiceTests
{
    private readonly Mock<ILogger<GitWorkspaceService>> _loggerMock;
    private readonly RepositoryProcessingSettings _settings;
    private readonly GitWorkspaceService _service;

    public GitWorkspaceServiceTests()
    {
        _loggerMock = new Mock<ILogger<GitWorkspaceService>>();
        _settings = new RepositoryProcessingSettings
        {
            WorkspaceRoot = Path.Combine(Path.GetTempPath(), "GodForgeTest"),
            MaxFiles = 10000,
            MaxRepositoryBytes = 100_000_000,
            MaxTextFileBytes = 10_000_000,
            GitCommandTimeoutSeconds = 30,
            AllowedRemoteHosts = new[] { "github.com", "gitlab.com" },
            AllowPrivateNetworkRemotes = false
        };

        var optionsMock = new Mock<IOptions<RepositoryProcessingSettings>>();
        optionsMock.Setup(o => o.Value).Returns(_settings);

        _service = new GitWorkspaceService(optionsMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task SyncAsync_WithNonHttpsUrl_ThrowsArgumentException()
    {
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.SyncAsync(Guid.NewGuid(), "http://github.com/a/b.git", "main"));
        Assert.Contains("Only absolute HTTPS Git URLs are supported", ex.Message);
    }

    [Fact]
    public async Task SyncAsync_WithEmbeddedCredentials_ThrowsArgumentException()
    {
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.SyncAsync(Guid.NewGuid(), "https://user:pass@github.com/a/b.git", "main"));
        Assert.Contains("Git URLs must not contain embedded credentials", ex.Message);
    }

    [Fact]
    public async Task SyncAsync_WithLocalhost_ThrowsInvalidOperationException()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.SyncAsync(Guid.NewGuid(), "https://localhost/repo.git", "main"));
        Assert.Contains("Private-network Git remotes are disabled", ex.Message);
    }

    [Fact]
    public async Task SyncAsync_WithUnresolvableHost_ThrowsInvalidOperationException()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.SyncAsync(Guid.NewGuid(), "https://this-domain-does-not-exist-12345.com/repo.git", "main"));
        Assert.Contains("The Git remote host could not be resolved", ex.Message);
    }

    [Fact]
    public async Task SyncAsync_WithInvalidBranchName_ThrowsArgumentException()
    {
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.SyncAsync(Guid.NewGuid(), "https://github.com/a/b.git", "-invalid-branch"));
        Assert.Contains("Branch name is not a safe Git branch reference", ex.Message);
    }
}
