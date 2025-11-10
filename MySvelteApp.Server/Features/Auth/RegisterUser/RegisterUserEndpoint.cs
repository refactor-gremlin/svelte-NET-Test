using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySvelteApp.Server.Features.Auth.RegisterUser;
using MySvelteApp.Server.Shared.Common.DTOs.Responses;
using MySvelteApp.Server.Shared.Presentation.Common;

namespace MySvelteApp.Server.Features.Auth.RegisterUser;

[ApiController]
[Route("auth/register")]
public class RegisterUserEndpoint : ApiControllerBase
{
    private readonly RegisterUserCommand _handler;

    public RegisterUserEndpoint(RegisterUserCommand handler)
    {
        _handler = handler;
    }

    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<RegisterUserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Handle(
        [FromBody] RegisterUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _handler.HandleAsync(request, cancellationToken);
        return ToActionResult(result);
    }
}

