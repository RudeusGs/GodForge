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
                return Ok(new ApiResponse<object> { Meta = new ApiMeta { CorrelationId = CorrelationId } });

            if (result.Value is IPagedResult pagedResult)
            {
                return Ok(new ApiPagedResponse<object>
                {
                    Data = pagedResult.ItemsObject,
                    Meta = new ApiPagedMeta
                    {
                        CorrelationId = CorrelationId,
                        Page = pagedResult.Page,
                        PageSize = pagedResult.PageSize,
                        TotalCount = pagedResult.TotalItems
                    }
                });
            }

            return Ok(new ApiResponse<T>
            {
                Data = result.Value,
                Meta = new ApiMeta { CorrelationId = CorrelationId }
            });
        }

        var errorResponse = CreateErrorResponse(result.Error!);
        return result.Error!.Type switch
        {
            ErrorType.NotFound => NotFound(errorResponse),
            ErrorType.Validation => BadRequest(errorResponse),
            ErrorType.Conflict => Conflict(errorResponse),
            ErrorType.Unauthorized => Unauthorized(errorResponse),
            ErrorType.Forbidden => StatusCode(StatusCodes.Status403Forbidden, errorResponse),
            _ => StatusCode(StatusCodes.Status500InternalServerError, errorResponse)
        };
    }

    protected ActionResult HandleResult(Result result)
    {
        if (result.IsSuccess)
        {
            return Ok(new ApiResponse<object> { Meta = new ApiMeta { CorrelationId = CorrelationId } });
        }

        var errorResponse = CreateErrorResponse(result.Error!);
        return result.Error!.Type switch
        {
            ErrorType.NotFound => NotFound(errorResponse),
            ErrorType.Validation => BadRequest(errorResponse),
            ErrorType.Conflict => Conflict(errorResponse),
            ErrorType.Unauthorized => Unauthorized(errorResponse),
            ErrorType.Forbidden => StatusCode(StatusCodes.Status403Forbidden, errorResponse),
            _ => StatusCode(StatusCodes.Status500InternalServerError, errorResponse)
        };
    }

    private ApiErrorResponse CreateErrorResponse(ApplicationError error)
    {
        return new ApiErrorResponse
        {
            Error = new ApiErrorDetail
            {
                Code = error.Code,
                Message = error.Message,
                CorrelationId = CorrelationId,
                Details = error.Details
            }
        };
    }
}

/// <summary>
/// Standard API response envelope.
/// </summary>
public class ApiResponse<T>
{
    /// <summary>
    /// The response payload.
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// Metadata about the response.
    /// </summary>
    public ApiMeta? Meta { get; set; }
}

/// <summary>
/// API response envelope for paginated collections.
/// </summary>
public class ApiPagedResponse<T>
{
    /// <summary>
    /// The paginated items.
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// Pagination metadata.
    /// </summary>
    public ApiPagedMeta? Meta { get; set; }
}

/// <summary>
/// Standard API metadata.
/// </summary>
public class ApiMeta
{
    /// <summary>
    /// A unique identifier for the request, used for tracing.
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;
}

/// <summary>
/// Metadata for paginated responses.
/// </summary>
public class ApiPagedMeta : ApiMeta
{
    /// <summary>
    /// The current page number (1-indexed).
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// The number of items per page.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// The total number of items available across all pages.
    /// </summary>
    public int TotalCount { get; set; }
}

/// <summary>
/// Standard API error response envelope.
/// </summary>
public class ApiErrorResponse
{
    /// <summary>
    /// Details about the error.
    /// </summary>
    public ApiErrorDetail Error { get; set; } = new();
}

/// <summary>
/// Details about an API error.
/// </summary>
public class ApiErrorDetail
{
    /// <summary>
    /// A stable, machine-readable error code.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// A human-readable error message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// The request correlation ID.
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// Additional error details (e.g., validation errors), if any.
    /// </summary>
    public object? Details { get; set; }
}
