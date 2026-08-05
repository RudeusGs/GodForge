namespace GodForge.Application.Common.Interfaces;

public interface IEmailOutbox
{
    Task EnqueueAsync(
        string recipient,
        string subject,
        string htmlBody,
        string correlationId,
        CancellationToken cancellationToken = default);
}
