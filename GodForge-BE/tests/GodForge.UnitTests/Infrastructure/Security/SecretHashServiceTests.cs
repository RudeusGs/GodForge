using GodForge.Infrastructure.Configuration;
using GodForge.Infrastructure.Security;
using Microsoft.Extensions.Options;

namespace GodForge.UnitTests.Infrastructure.Security;

public sealed class SecretHashServiceTests
{
    [Fact]
    public void Hash_UsesDedicatedConfiguredKey()
    {
        var first = CreateService("first-secret-hash-key-with-at-least-32-characters");
        var second = CreateService("second-secret-hash-key-with-at-least-32-characters");

        var firstHash = first.Hash("challenge-token");
        var secondHash = second.Hash("challenge-token");

        Assert.NotEqual(firstHash, secondHash);
        Assert.True(first.Verify("challenge-token", firstHash));
        Assert.False(first.Verify("different-token", firstHash));
    }

    [Fact]
    public void Verify_AcceptsLegacyHashDuringKeyMigration()
    {
        const string legacyKey = "legacy-jwt-key-with-at-least-32-characters";
        var legacyService = CreateService(legacyKey);
        var migratedService = CreateService(
            "new-secret-hash-key-with-at-least-32-characters",
            legacyKey);

        var legacyHash = legacyService.Hash("outstanding-token");

        Assert.True(migratedService.Verify("outstanding-token", legacyHash));
        Assert.NotEqual(legacyHash, migratedService.Hash("outstanding-token"));
    }

    private static SecretHashService CreateService(string key, string legacyKey = "")
        => new(Options.Create(new SecretHashSettings { Key = key, LegacyKey = legacyKey }));
}
