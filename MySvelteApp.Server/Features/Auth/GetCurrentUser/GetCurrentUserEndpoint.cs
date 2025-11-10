using Microsoft.AspNetCore.Mvc;
using MySvelteApp.Server.Features.Auth.GetCurrentUser;
using MySvelteApp.Server.Shared.Common.DTOs.Responses;
using MySvelteApp.Server.Shared.Presentation.Common;

namespace MySvelteApp.Server.Features.Auth.GetCurrentUser;

[ApiController]
[Route("auth/me")]
public class GetCurrentUserEndpoint : ApiControllerBase
{
    private readonly GetCurrentUserHandler _handler;

    public GetCurrentUserEndpoint(GetCurrentUserHandler handler)
    {
        _handler = handler;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<GetCurrentUserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Handle(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return UnauthorizedError("Invalid token.");
        }

        var result = await _handler.HandleAsync(userId.Value, cancellationToken);
        return ToActionResult(result);
    }
}

