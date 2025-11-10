using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySvelteApp.Server.Features.Auth.LoginUser;
using MySvelteApp.Server.Shared.Common.DTOs.Responses;
using MySvelteApp.Server.Shared.Presentation.Common;

namespace MySvelteApp.Server.Features.Auth.LoginUser;

[ApiController]
[Route("auth/login")]
public class LoginUserEndpoint : ApiControllerBase
{
    private readonly LoginUserHandler _handler;

    public LoginUserEndpoint(LoginUserHandler handler)
    {
        _handler = handler;
    }

    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<LoginUserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Handle(
        [FromBody] LoginUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _handler.HandleAsync(request, cancellationToken);
        return ToActionResult(result);
    }
}

