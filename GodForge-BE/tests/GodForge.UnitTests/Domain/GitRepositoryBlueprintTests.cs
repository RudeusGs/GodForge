using GodForge.Domain.Entities.Repo;
using GodForge.Domain.Enums;

namespace GodForge.UnitTests.Domain;

public sealed class GitRepositoryBlueprintTests
{
    [Fact]
    public void CreateLinked_RemovesEmbeddedCredentialsFromExposedCloneUrl()
    {
        var repository = GitRepository.CreateLinked(
            Guid.NewGuid(),
            "https://user:secret@example.com/team/game.git",
            GitProvider.Generic,
            "main",
            null,
            false,
            DateTimeOffset.UtcNow);

        Assert.Equal(RepositoryMode.ExternalLinked, repository.Mode);
        Assert.DoesNotContain("user", repository.CloneUrlSanitized);
        Assert.DoesNotContain("secret", repository.CloneUrlSanitized);
        Assert.Equal(GitRepositoryStatus.Configured, repository.GitRepositoryStatus);
    }

    [Fact]
    public void MarkSynchronized_StoresRevisionAndReadyState()
    {
        var now = DateTimeOffset.UtcNow;
        var repository = GitRepository.CreateLinked(
            Guid.NewGuid(),
            "https://example.com/team/game.git",
            GitProvider.Generic,
            "main",
            null,
            true,
            now);

        repository.MarkSynchronized("0123456789abcdef", 1234, now.AddMinutes(1));

        Assert.Equal(GitRepositoryStatus.Ready, repository.GitRepositoryStatus);
        Assert.Equal("0123456789abcdef", repository.CurrentCommitHash);
        Assert.Equal(1234, repository.RepoSizeBytes);
        Assert.NotNull(repository.LastSyncedAt);
    }
}
