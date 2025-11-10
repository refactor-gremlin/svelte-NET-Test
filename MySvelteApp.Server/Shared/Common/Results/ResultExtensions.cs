using MySvelteApp.Server.Shared.Common.Results;

namespace MySvelteApp.Server.Shared.Common.Results;

public static class ResultExtensions
{
    public static Result<T> ToResult<T>(this T value)
    {
        return Result<T>.Success(value);
    }

    public static Result<T> ToFailureResult<T>(this string error, string? errorCode = null)
    {
        return Result<T>.Failure(error, errorCode);
    }

    public static Result<TResult> Map<T, TResult>(
        this Result<T> result,
        Func<T, TResult> mapper)
    {
        if (result.IsFailure)
        {
            return Result<TResult>.Failure(result.Error!, result.ErrorCode);
        }

        return Result<TResult>.Success(mapper(result.Value!));
    }

    public static async Task<Result<TResult>> MapAsync<T, TResult>(
        this Task<Result<T>> resultTask,
        Func<T, Task<TResult>> mapper)
    {
        var result = await resultTask;
        if (result.IsFailure)
        {
            return Result<TResult>.Failure(result.Error!, result.ErrorCode);
        }

        var mappedValue = await mapper(result.Value!);
        return Result<TResult>.Success(mappedValue);
    }
}

