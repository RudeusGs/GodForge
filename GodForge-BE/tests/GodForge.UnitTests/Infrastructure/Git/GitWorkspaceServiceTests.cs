using System.Net;
using GodForge.Application.Common.Interfaces;
using GodForge.Infrastructure.Configuration;
using GodForge.Infrastructure.Git;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace GodForge.UnitTests.Infrastructure.Git;

public sealed class GitWorkspaceServiceTests
{
    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("10.0.0.1")]
    [InlineData("100.64.0.1")]
    [InlineData("169.254.10.20")]
    [InlineData("172.16.0.1")]
    [InlineData("192.168.1.1")]
    [InlineData("198.18.0.1")]
    [InlineData("203.0.113.10")]
    [InlineData("::1")]
    [InlineData("fc00::1")]
    [InlineData("fe80::1")]
    [InlineData("2001:db8::1")]
    public void IsRestrictedAddress_RejectsNonPublicRanges(string value)
    {
        Assert.True(GitWorkspaceService.IsRestrictedAddress(IPAddress.Parse(value)));
    }

    [Theory]
    [InlineData("1.1.1.1")]
    [InlineData("8.8.8.8")]
    [InlineData("2606:4700:4700::1111")]
    public void IsRestrictedAddress_AllowsPublicAddresses(string value)
    {
        Assert.False(GitWorkspaceService.IsRestrictedAddress(IPAddress.Parse(value)));
    }

    [Fact]
    public void IsRestrictedAddress_NormalizesIpv4MappedIpv6()
    {
        Assert.True(GitWorkspaceService.IsRestrictedAddress(IPAddress.Parse("::ffff:127.0.0.1")));
    }

    [Fact]
    public async Task ExceedsNullDelimitedEntryLimitAsync_StopsAfterLimitIsExceeded()
    {
        await using var stream = new MemoryStream("one\0two\0three\0"u8.ToArray());

        var exceeded = await GitWorkspaceService.ExceedsNullDelimitedEntryLimitAsync(stream, 2);

        Assert.True(exceeded);
    }

    [Fact]
    public async Task ExceedsNullDelimitedEntryLimitAsync_AllowsEntriesAtConfiguredLimit()
    {
        await using var stream = new MemoryStream("one\0two\0"u8.ToArray());

        var exceeded = await GitWorkspaceService.ExceedsNullDelimitedEntryLimitAsync(stream, 2);

        Assert.False(exceeded);
    }

    [Fact]
    public void GetNextLimitMonitorInterval_UsesBoundedExponentialBackoff()
    {
        Assert.Equal(TimeSpan.FromSeconds(10), GitWorkspaceService.GetNextLimitMonitorInterval(TimeSpan.FromSeconds(5)));
        Assert.Equal(TimeSpan.FromSeconds(20), GitWorkspaceService.GetNextLimitMonitorInterval(TimeSpan.FromSeconds(10)));
        Assert.Equal(TimeSpan.FromSeconds(30), GitWorkspaceService.GetNextLimitMonitorInterval(TimeSpan.FromSeconds(20)));
        Assert.Equal(TimeSpan.FromSeconds(30), GitWorkspaceService.GetNextLimitMonitorInterval(TimeSpan.FromSeconds(30)));
    }

    [Fact]
    public async Task SyncAsync_WhenDistributedLockFails_ReleasesRepositoryLockEntry()
    {
        var repositoryLockProvider = new Mock<IRepositoryLockProvider>();
        repositoryLockProvider
            .Setup(provider => provider.AcquireAsync(
                It.IsAny<Guid>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());
        var settings = Options.Create(new RepositoryProcessingSettings
        {
            WorkspaceRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")),
            GitCommandTimeoutSeconds = 30
        });
        var service = new GitWorkspaceService(
            settings,
            repositoryLockProvider.Object,
            NullLogger<GitWorkspaceService>.Instance);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.SyncAsync(
            Guid.NewGuid(),
            "https://github.com/example/repository.git",
            "main",
            CancellationToken.None));

        Assert.Equal(0, GitWorkspaceService.RepositoryLockCount);
        repositoryLockProvider.Verify(
            provider => provider.AcquireAsync(It.IsAny<Guid>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
