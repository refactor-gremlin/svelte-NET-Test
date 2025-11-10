using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySvelteApp.Server.Shared.Common.Results;
using MySvelteApp.Server.Shared.Presentation.Common;
using System.Security.Claims;

namespace MySvelteApp.Server.Shared.Presentation.Common;

public abstract class ApiControllerBase : ControllerBase
{
    protected IActionResult ToActionResult<T>(ApiResult<T> result)
    {
        return result.ToActionResult();
    }

    protected int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            return null;
        }
        return userId;
    }

    protected IActionResult UnauthorizedError(string message = "Unauthorized")
    {
        return Unauthorized(new Shared.Common.DTOs.Responses.ApiErrorResponse { Message = message });
    }
}

