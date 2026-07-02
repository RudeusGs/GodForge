using FluentValidation;
using GodForge.Application.Common.Models;
using MediatR;

namespace GodForge.Application.Common.Behaviors;

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Count != 0)
        {
            var errors = string.Join("; ", failures.Select(f => $"{f.PropertyName}: {f.ErrorMessage}"));

            // If TResponse is Result<T> or Result, we should return a failed result
            var responseType = typeof(TResponse);
            if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
            {
                var error = ApplicationError.Validation("VALIDATION_ERROR", "Validation failed", errors);
                var genericType = responseType.GetGenericArguments()[0];
                var failureMethod = typeof(Result).GetMethod("Failure")!.MakeGenericMethod(genericType);
                return (TResponse)failureMethod.Invoke(null, new object[] { error })!;
            }
            if (responseType == typeof(Result))
            {
                var error = ApplicationError.Validation("VALIDATION_ERROR", "Validation failed", errors);
                return (TResponse)(object)Result.Failure(error);
            }

            throw new ValidationException(failures);
        }

        return await next();
    }
}
