using GodForge.Infrastructure.Security;

namespace GodForge.UnitTests.Infrastructure.Security;

public sealed class SecretRedactorTests
{
    [Theory]
    [InlineData("API_KEY=abc123456789", "abc123456789")]
    [InlineData("password: super-secret-password", "super-secret-password")]
    [InlineData("Authorization: Bearer abcdefghijklmnopqrstuvwxyz", "abcdefghijklmnopqrstuvwxyz")]
    [InlineData("{\"apiKey\":\"AIzaabcdefghijklmnopqrstuvwxyz123456\"}", "AIzaabcdefghijklmnopqrstuvwxyz123456")]
    [InlineData("Host=localhost;Password=database-secret;Database=godforge", "database-secret")]
    [InlineData("postgresql://godforge:database-secret@localhost/godforge", "database-secret")]
    public void Redact_RemovesCommonSecretValues(string input, string secret)
    {
        var redactor = new SecretRedactor();

        var output = redactor.Redact(input);

        Assert.False(output.Contains(secret, StringComparison.Ordinal));
        Assert.True(output.Contains("REDACTED_SECRET", StringComparison.Ordinal));
    }

    [Fact]
    public void Redact_PreservesConnectionStringStructure()
    {
        var redactor = new SecretRedactor();

        var output = redactor.Redact("postgresql://godforge:secret-value@localhost/godforge");

        Assert.Equal("postgresql://godforge:[REDACTED_SECRET]@localhost/godforge", output);
    }
}
