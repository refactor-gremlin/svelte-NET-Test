namespace MySvelteApp.Server.Shared.Common.Results;

public class ApiResult<T>
{
    public bool IsSuccess { get; private set; }
    public bool IsFailure => !IsSuccess;
    public T? Value { get; private set; }
    public string? ErrorMessage { get; private set; }
    public ApiErrorType ErrorType { get; private set; } = ApiErrorType.None;

    private ApiResult(bool isSuccess, T? value, string? errorMessage, ApiErrorType errorType)
    {
        IsSuccess = isSuccess;
        Value = value;
        ErrorMessage = errorMessage;
        ErrorType = errorType;
    }

    public static ApiResult<T> Success(T value)
    {
        return new ApiResult<T>(true, value, null, ApiErrorType.None);
    }

    public static ApiResult<T> Failure(string errorMessage, ApiErrorType errorType = ApiErrorType.BadRequest)
    {
        return new ApiResult<T>(false, default, errorMessage, errorType);
    }

    public static ApiResult<T> Unauthorized(string errorMessage = "Unauthorized")
    {
        return new ApiResult<T>(false, default, errorMessage, ApiErrorType.Unauthorized);
    }

    public static ApiResult<T> Conflict(string errorMessage)
    {
        return new ApiResult<T>(false, default, errorMessage, ApiErrorType.Conflict);
    }

    public static ApiResult<T> ValidationError(string errorMessage)
    {
        return new ApiResult<T>(false, default, errorMessage, ApiErrorType.Validation);
    }

    public static ApiResult<T> NotFound(string errorMessage = "Resource not found")
    {
        return new ApiResult<T>(false, default, errorMessage, ApiErrorType.NotFound);
    }
}

public enum ApiErrorType
{
    None = 0,
    Validation = 1,
    Conflict = 2,
    Unauthorized = 3,
    BadRequest = 4,
    NotFound = 5,
    InternalServerError = 6
}

