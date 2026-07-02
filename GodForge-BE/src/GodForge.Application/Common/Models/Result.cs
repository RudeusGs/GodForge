namespace GodForge.Application.Common.Models;

public class Result
{
    public bool IsSuccess { get; }
    public bool IsError => !IsSuccess;
    public ApplicationError? Error { get; }

    protected Result(bool isSuccess, ApplicationError? error)
    {
        if (isSuccess && error != null)
            throw new InvalidOperationException("Success result cannot have an error.");
        if (!isSuccess && error == null)
            throw new InvalidOperationException("Error result must have an error.");

        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, null);
    public static Result Failure(ApplicationError error) => new(false, error);

    public TResult Match<TResult>(Func<TResult> onSuccess, Func<ApplicationError, TResult> onFailure)
    {
        return IsSuccess ? onSuccess() : onFailure(Error!);
    }
}

public sealed class Result<TValue> : Result
{
    private readonly TValue? _value;

    public TValue Value => IsSuccess ? _value! : throw new InvalidOperationException("Cannot access value of a failed result.");

    private Result(TValue? value, bool isSuccess, ApplicationError? error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    public static Result<TValue> Success(TValue value) => new(value, true, null);
    public new static Result<TValue> Failure(ApplicationError error) => new(default, false, error);

    public TResult Match<TResult>(Func<TValue, TResult> onSuccess, Func<ApplicationError, TResult> onFailure)
    {
        return IsSuccess ? onSuccess(Value) : onFailure(Error!);
    }

    public static implicit operator Result<TValue>(TValue value) => Success(value);
    public static implicit operator Result<TValue>(ApplicationError error) => Failure(error);
}
