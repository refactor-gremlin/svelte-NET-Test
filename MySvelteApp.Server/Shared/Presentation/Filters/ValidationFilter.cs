using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MySvelteApp.Server.Shared.Common.DTOs.Responses;

namespace MySvelteApp.Server.Shared.Presentation.Filters;

public class ValidationFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            var errors = context.ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .SelectMany(x => x.Value!.Errors.Select(e => e.ErrorMessage))
                .ToList();

            var errorMessage = string.Join(" ", errors);
            context.Result = new BadRequestObjectResult(new ApiErrorResponse
            {
                Message = errorMessage,
                ErrorCode = "Validation"
            });
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        // No action needed after execution
    }
}

