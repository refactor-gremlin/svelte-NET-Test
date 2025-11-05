using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySvelteApp.Server.Application.Authentication;
using MySvelteApp.Server.Application.Authentication.DTOs;
using MySvelteApp.Server.Presentation.Models.Auth;

namespace MySvelteApp.Server.Presentation.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthSuccessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.RegisterAsync(request, cancellationToken);
        if (!result.Success)
        {
            return ToErrorResponse(result);
        }

        return Ok(new AuthSuccessResponse
        {
            Token = result.Token ?? string.Empty,
            UserId = result.UserId ?? 0,
            Username = result.Username ?? string.Empty
        });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthSuccessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(AuthErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(request, cancellationToken);
        if (!result.Success)
        {
            return ToErrorResponse(result);
        }

        return Ok(new AuthSuccessResponse
        {
            Token = result.Token ?? string.Empty,
            UserId = result.UserId ?? 0,
            Username = result.Username ?? string.Empty
        });
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(CurrentUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized(new AuthErrorResponse { Message = "Invalid token." });
        }

        var currentUser = await _authService.GetCurrentUserAsync(userId, cancellationToken);
        if (currentUser == null)
        {
            return Unauthorized(new AuthErrorResponse { Message = "User not found." });
        }

        return Ok(currentUser);
    }

    private IActionResult ToErrorResponse(AuthResult result)
    {
        var errorMessage = result.ErrorMessage ?? "An unknown error occurred.";
        return result.ErrorType switch
        {
            AuthErrorType.Unauthorized => Unauthorized(new AuthErrorResponse { Message = errorMessage }),
            _ => BadRequest(new AuthErrorResponse { Message = errorMessage })
        };
    }
}
