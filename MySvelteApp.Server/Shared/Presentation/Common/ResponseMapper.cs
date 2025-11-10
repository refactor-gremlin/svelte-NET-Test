using Microsoft.AspNetCore.Mvc;
using MySvelteApp.Server.Shared.Common.DTOs.Responses;
using MySvelteApp.Server.Shared.Common.Results;

namespace MySvelteApp.Server.Shared.Presentation.Common;

public static class ResponseMapper
{
    public static IActionResult ToActionResult<T>(this ApiResult<T> result)
    {
        if (result.IsSuccess)
        {
            return new OkObjectResult(new ApiResponse<T> { Data = result.Value!, Success = true });
        }

        var errorResponse = new ApiErrorResponse
        {
            Message = result.ErrorMessage ?? "An error occurred",
            ErrorCode = result.ErrorType.ToString()
        };

        return result.ErrorType switch
        {
            ApiErrorType.Unauthorized => new UnauthorizedObjectResult(errorResponse),
            ApiErrorType.Conflict => new ConflictObjectResult(errorResponse),
            ApiErrorType.Validation => new BadRequestObjectResult(errorResponse),
            ApiErrorType.NotFound => new NotFoundObjectResult(errorResponse),
            _ => new BadRequestObjectResult(errorResponse)
        };
    }
}

