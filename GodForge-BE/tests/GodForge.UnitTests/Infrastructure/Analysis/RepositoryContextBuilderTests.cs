using GodForge.Infrastructure.Analysis;
using GodForge.Infrastructure.Configuration;
using GodForge.Infrastructure.Security;
using Microsoft.Extensions.Options;

namespace GodForge.UnitTests.Infrastructure.Analysis;

public sealed class RepositoryContextBuilderTests
{
    [Fact]
    public async Task BuildAsync_ExcludesEnvironmentFilesAndRedactsSecrets()
    {
        var root = Path.Combine(Path.GetTempPath(), "godforge-context-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            await File.WriteAllTextAsync(Path.Combine(root, "project.godot"), "[application]\nconfig/name=\"Demo\"");
            await File.WriteAllTextAsync(Path.Combine(root, "main.gd"), "API_KEY=abcdefghijklmnop\nprint(\"hello\")");
            await File.WriteAllTextAsync(Path.Combine(root, ".env.production"), "PASSWORD=must-not-leak");

            var builder = new RepositoryContextBuilder(
                Options.Create(new RepositoryProcessingSettings
                {
                    MaxFiles = 100,
                    MaxTextFileBytes = 1024,
                    MaxContextCharacters = 100_000
                }),
                new SecretRedactor());

            var result = await builder.BuildAsync(root);

            Assert.DoesNotContain("must-not-leak", result.Content);
            Assert.DoesNotContain(".env.production", result.Content);
            Assert.DoesNotContain("abcdefghijklmnop", result.Content);
            Assert.Contains("REDACTED_SECRET", result.Content);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
