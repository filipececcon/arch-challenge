namespace ArchChallenge.CashFlow.Domain.Shared.Notifications;

public class Result
{
    protected Result(bool isSuccess, IReadOnlyList<Notification> errors)
    {
        IsSuccess = isSuccess;
        Errors = errors;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public IReadOnlyList<Notification> Errors { get; }

    public static Result Success() => new(true, []);

    public static Result Failure(params Notification[] errors) => new(false, errors);

    public static Result Failure(IReadOnlyList<Notification> errors) => new(false, errors);
}

public sealed class Result<T> : Result
{
    private Result(T value) : base(true, []) => Value = value;

    private Result(IReadOnlyList<Notification> errors) : base(false, errors) => Value = default;

    public T? Value { get; }

    public static Result<T> Success(T value) => new(value);

    public new static Result<T> Failure(params Notification[] errors) => new((IReadOnlyList<Notification>)errors);

    public new static Result<T> Failure(IReadOnlyList<Notification> errors) => new(errors);
}
