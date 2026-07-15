namespace GodForge.Application.Common.Interfaces;

public interface ISecretRedactor
{
    string Redact(string content);
}
