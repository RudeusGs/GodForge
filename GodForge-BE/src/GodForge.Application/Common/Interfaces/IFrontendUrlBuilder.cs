namespace GodForge.Application.Common.Interfaces;

public interface IFrontendUrlBuilder
{
    string BuildPasswordResetUrl(string email, string token);
}
