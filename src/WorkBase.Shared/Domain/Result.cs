namespace WorkBase.Shared.Domain;

public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }
    public IReadOnlyList<ValidationError> ValidationErrors { get; }

    protected Result(bool isSuccess, Error error, IReadOnlyList<ValidationError>? validationErrors = null)
    {
        IsSuccess = isSuccess;
        Error = error;
        ValidationErrors = validationErrors ?? [];
    }

    public static Result Success() =>
        new(true, Error.None);

    public static Result Failure(Error error) =>
        new(false, error);

    public static Result ValidationFailure(IReadOnlyList<ValidationError> errors) =>
        new(false, Error.Validation("ValidationError", "One or more validation errors occurred."), errors);

    public static Result<T> Success<T>(T value) =>
        new(value);

    public static Result<T> Failure<T>(Error error) =>
        new(error);

    public static Result<T> ValidationFailure<T>(IReadOnlyList<ValidationError> errors) =>
        new(Error.Validation("ValidationError", "One or more validation errors occurred."), errors);
}

public class Result<T> : Result
{
    private readonly T? _value;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access Value on a failed result.");

    internal Result(T value) : base(true, Error.None) => _value = value;
    internal Result(Error error) : base(false, error) { }
    internal Result(Error error, IReadOnlyList<ValidationError> validationErrors)
        : base(false, error, validationErrors) { }

    public static implicit operator Result<T>(T value) => new(value);

    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<Result<T>, TResult> onFailure)
        => IsSuccess ? onSuccess(Value) : onFailure(this);
}
