namespace MySvelteApp.Server.Shared.Common.Results;

public class Result<T>
{
    public bool IsSuccess { get; private set; }
    public bool IsFailure => !IsSuccess;
    public T? Value { get; private set; }
    public string? Error { get; private set; }
    public string? ErrorCode { get; private set; }

    private Result(bool isSuccess, T? value, string? error, string? errorCode)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
        ErrorCode = errorCode;
    }

    public static Result<T> Success(T value)
    {
        return new Result<T>(true, value, null, null);
    }

    public static Result<T> Failure(string error, string? errorCode = null)
    {
        return new Result<T>(false, default, error, errorCode);
    }
}

public class Result
{
    public bool IsSuccess { get; private set; }
    public bool IsFailure => !IsSuccess;
    public string? Error { get; private set; }
    public string? ErrorCode { get; private set; }

    private Result(bool isSuccess, string? error, string? errorCode)
    {
        IsSuccess = isSuccess;
        Error = error;
        ErrorCode = errorCode;
    }

    public static Result Success()
    {
        return new Result(true, null, null);
    }

    public static Result Failure(string error, string? errorCode = null)
    {
        return new Result(false, error, errorCode);
    }
}

