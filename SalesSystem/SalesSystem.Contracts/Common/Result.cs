namespace SalesSystem.Contracts.Common;

public class Result
{
    public bool IsSuccess { get; protected set; }
    public string? Error { get; protected set; }
    public string? ErrorCode { get; protected set; }

    protected Result() { }

    public static Result Success() => new Result { IsSuccess = true };

    public static Result Failure(string error, string? errorCode = null) => new Result
    {
        IsSuccess = false,
        Error = error,
        ErrorCode = errorCode
    };
}

public class Result<T> : Result
{
    public T? Value { get; private set; }

    protected Result() { }

    public static Result<T> Success(T value) => new Result<T> { IsSuccess = true, Value = value };

    public static new Result<T> Failure(string error, string? errorCode = null) => new Result<T>
    {
        IsSuccess = false,
        Error = error,
        ErrorCode = errorCode
    };
}