using System.Net;
using System.Text.Json;
using MySvelteApp.Server.Shared.Common.DTOs.Responses;

namespace MySvelteApp.Server.Shared.Presentation.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var code = HttpStatusCode.InternalServerError;
        var message = "An error occurred while processing your request.";
        var errorCode = "InternalServerError";

        // Handle specific exception types
        switch (exception)
        {
            case ArgumentNullException:
            case ArgumentException:
                code = HttpStatusCode.BadRequest;
                message = exception.Message;
                errorCode = "BadRequest";
                break;
            case UnauthorizedAccessException:
                code = HttpStatusCode.Unauthorized;
                message = "You are not authorized to perform this action.";
                errorCode = "Unauthorized";
                break;
            case InvalidOperationException:
                code = HttpStatusCode.BadRequest;
                message = exception.Message;
                errorCode = "BadRequest";
                break;
        }

        var result = JsonSerializer.Serialize(new ApiErrorResponse 
        { 
            Message = message,
            ErrorCode = errorCode
        });
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)code;
        return context.Response.WriteAsync(result);
    }
}

