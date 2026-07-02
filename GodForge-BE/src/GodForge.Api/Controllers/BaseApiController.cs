using GodForge.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace GodForge.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    protected string CorrelationId => HttpContext.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString("N");

    protected ActionResult HandleResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
        {
            if (result.Value is null)
                return NoContent();

            if (result.Value is IPagedResult pagedResult)
            {
                return Ok(new
                {
                    data = pagedResult.ItemsObject,
                    meta = new
                    {
                        correlationId = CorrelationId,
                        page = pagedResult.Page,
                        pageSize = pagedResult.PageSize,
                        totalCount = pagedResult.TotalItems
                    }
                });
            }

            return Ok(new
            {
                data = result.Value,
                meta = new { correlationId = CorrelationId }
            });
        }

        return result.Error!.Type switch
        {
            ErrorType.NotFound => NotFound(CreateErrorResponse(result.Error)),
            ErrorType.Validation => BadRequest(CreateErrorResponse(result.Error)),
            ErrorType.Conflict => Conflict(CreateErrorResponse(result.Error)),
            ErrorType.Unauthorized => Unauthorized(CreateErrorResponse(result.Error)),
            ErrorType.Forbidden => StatusCode(StatusCodes.Status403Forbidden, CreateErrorResponse(result.Error)),
            _ => StatusCode(StatusCodes.Status500InternalServerError, CreateErrorResponse(result.Error))
        };
    }

    private object CreateErrorResponse(ApplicationError error)
    {
        return new
        {
            error = new
            {
                code = error.Code,
                message = error.Message,
                correlationId = CorrelationId,
                details = error.Details
            }
        };
    }
}
