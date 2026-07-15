using GodForge.Infrastructure.Security;

namespace GodForge.UnitTests.Infrastructure.Security;

public sealed class SecretRedactorTests
{
    [Theory]
    [InlineData("API_KEY=abc123456789", "abc123456789")]
    [InlineData("password: super-secret-password", "super-secret-password")]
    [InlineData("Authorization: Bearer abcdefghijklmnopqrstuvwxyz", "abcdefghijklmnopqrstuvwxyz")]
    public void Redact_RemovesCommonSecretValues(string input, string secret)
    {
        var redactor = new SecretRedactor();

        var output = redactor.Redact(input);

        Assert.DoesNotContain(secret, output);
        Assert.Contains("REDACTED_SECRET", output);
    }
}
