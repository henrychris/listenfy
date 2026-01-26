namespace Listenfy.Shared.Results;

// Note: the constructor is public so the JSON serializer can access it.
public class Result<T>
{
    private readonly T _value;

    public Result(bool isSuccess, T value, Error error)
    {
        if (isSuccess && error != Error.None || !isSuccess && error == Error.None)
        {
            throw new ArgumentException("Invalid error", nameof(error));
        }

        IsSuccess = isSuccess;
        _value = isSuccess ? value : default!;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    public T Value
    {
        get { return IsSuccess ? _value : default!; }
    }

    public static Result<T> Success(T value)
    {
        return new(true, value, Error.None);
    }

    public static Result<T> Failure(Error error)
    {
        return new(false, default!, error);
    }

    public static Result<T> Failure(List<Error> errors)
    {
        if (errors.Count == 0)
        {
            throw new ArgumentException("Error list cannot be empty.", nameof(errors));
        }
        var aggregatedError = errors.First();
        return new Result<T>(false, default!, aggregatedError);
    }
}
