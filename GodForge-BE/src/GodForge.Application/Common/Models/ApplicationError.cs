namespace GodForge.Application.Common.Models;

public sealed record ApplicationError(string Code, string Message, ErrorType Type, object? Details = null)
{
    public static ApplicationError Validation(string code, string message, object? details = null) 
        => new(code, message, ErrorType.Validation, details);

    public static ApplicationError NotFound(string code, string message) 
        => new(code, message, ErrorType.NotFound);

    public static ApplicationError Conflict(string code, string message) 
        => new(code, message, ErrorType.Conflict);

    public static ApplicationError Unauthorized(string code, string message) 
        => new(code, message, ErrorType.Unauthorized);

    public static ApplicationError Forbidden(string code, string message) 
        => new(code, message, ErrorType.Forbidden);

    public static ApplicationError Internal(string code, string message) 
        => new(code, message, ErrorType.Internal);
}
