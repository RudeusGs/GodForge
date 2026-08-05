namespace GodForge.Application.Common.Interfaces;

public interface ISecretHashService
{
    string Hash(string secret);
    bool Verify(string secret, string expectedHash);
}
