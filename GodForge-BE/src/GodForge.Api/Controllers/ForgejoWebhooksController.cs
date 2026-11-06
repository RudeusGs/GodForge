using GodForge.Application.Features.Repositories.Commands.ReceiveForgejoWebhook;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GodForge.Api.Controllers;

[AllowAnonymous]
public sealed class ForgejoWebhooksController(IMediator mediator) : BaseApiController
{
    [HttpPost("/api/v1/webhooks/forgejo")]
    [RequestSizeLimit(1048576)]
    [Consumes("application/json")]
    public async Task<IActionResult> Receive(CancellationToken cancellationToken)
    {
        using var body = new MemoryStream();
        var chunk = new byte[8192];
        int read;
        while ((read = await Request.Body.ReadAsync(chunk, cancellationToken)) > 0)
        {
            if (body.Length + read > 1048576) return StatusCode(StatusCodes.Status413PayloadTooLarge);
            body.Write(chunk, 0, read);
        }
        var result = await mediator.Send(new ReceiveForgejoWebhookCommand(body.ToArray(), Request.Headers["X-Forgejo-Signature"].ToString(),
            Request.Headers["X-Forgejo-Delivery"].ToString(), Request.Headers["X-Forgejo-Event"].ToString(), CorrelationId), cancellationToken);
        return result.IsSuccess ? Accepted(new ApiResponse<object> { Data = result.Value, Meta = new ApiMeta { CorrelationId = CorrelationId } }) : HandleResult(result);
    }
}
