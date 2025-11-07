using Microsoft.AspNetCore.Mvc;
using Moq;
using FluentAssertions;
using MySvelteApp.Server.Presentation.Controllers;
using MySvelteApp.Server.Application.Authentication;
using MySvelteApp.Server.Application.Authentication.DTOs;
using MySvelteApp.Server.Presentation.Models.Auth;
using MySvelteApp.Server.Tests.TestFixtures;

namespace MySvelteApp.Server.Tests.Presentation.Controllers;

public class AuthControllerTests : ControllerTestTemplate<AuthController>
{
    private readonly Mock<IAuthService> _mockAuthService;

    public AuthControllerTests()
    {
        _mockAuthService = new Mock<IAuthService>();
    }

    protected override AuthController CreateController()
    {
        return new AuthController(_mockAuthService.Object);
    }

    [Fact]
    public async Task Register_ValidRequest_ReturnsOkWithAuthSuccessResponse()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "Password123"
        };

        var authResult = new AuthResult
        {
            Success = true,
            Token = "test-token",
            UserId = 1,
            Username = "testuser"
        };

        _mockAuthService.Setup(x => x.RegisterAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(authResult);

        // Act
        var result = await Controller.Register(request, CancellationToken.None);

        // Assert
        var response = ControllerAssertionUtilities.AssertOkResult<AuthSuccessResponse>(result);
        response.Token.Should().Be(authResult.Token);
        response.UserId.Should().Be(authResult.UserId);
        response.Username.Should().Be(authResult.Username);
    }

    [Fact]
    public async Task Register_ValidationError_ReturnsBadRequestWithAuthErrorResponse()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "Password123"
        };

        var authResult = new AuthResult
        {
            Success = false,
            ErrorMessage = "Username is required",
            ErrorType = AuthErrorType.Validation
        };

        _mockAuthService.Setup(x => x.RegisterAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(authResult);

        // Act
        var result = await Controller.Register(request, CancellationToken.None);

        // Assert
        ControllerAssertionUtilities.AssertBadRequestResult(result, authResult.ErrorMessage);
    }

    [Fact]
    public async Task Register_ConflictError_ReturnsBadRequestWithAuthErrorResponse()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "Password123"
        };

        var authResult = new AuthResult
        {
            Success = false,
            ErrorMessage = "Username already exists",
            ErrorType = AuthErrorType.Conflict
        };

        _mockAuthService.Setup(x => x.RegisterAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(authResult);

        // Act
        var result = await Controller.Register(request, CancellationToken.None);

        // Assert
        ControllerAssertionUtilities.AssertBadRequestResult(result, authResult.ErrorMessage);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsOkWithAuthSuccessResponse()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = "testuser",
            Password = "Password123"
        };

        var authResult = new AuthResult
        {
            Success = true,
            Token = "test-token",
            UserId = 1,
            Username = "testuser"
        };

        _mockAuthService.Setup(x => x.LoginAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(authResult);

        // Act
        var result = await Controller.Login(request, CancellationToken.None);

        // Assert
        var response = ControllerAssertionUtilities.AssertOkResult<AuthSuccessResponse>(result);
        response.Token.Should().Be(authResult.Token);
        response.UserId.Should().Be(authResult.UserId);
        response.Username.Should().Be(authResult.Username);
    }

    [Fact]
    public async Task Login_ValidationError_ReturnsBadRequestWithAuthErrorResponse()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = "",
            Password = ""
        };

        var authResult = new AuthResult
        {
            Success = false,
            ErrorMessage = "Username is required",
            ErrorType = AuthErrorType.Validation
        };

        _mockAuthService.Setup(x => x.LoginAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(authResult);

        // Act
        var result = await Controller.Login(request, CancellationToken.None);

        // Assert
        ControllerAssertionUtilities.AssertBadRequestResult(result, authResult.ErrorMessage);
    }

    [Fact]
    public async Task Login_UnauthorizedError_ReturnsUnauthorizedWithAuthErrorResponse()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = "testuser",
            Password = "wrongpassword"
        };

        var authResult = new AuthResult
        {
            Success = false,
            ErrorMessage = "Invalid username or password",
            ErrorType = AuthErrorType.Unauthorized
        };

        _mockAuthService.Setup(x => x.LoginAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(authResult);

        // Act
        var result = await Controller.Login(request, CancellationToken.None);

        // Assert
        ControllerAssertionUtilities.AssertUnauthorizedResult(result, authResult.ErrorMessage);
    }

    [Fact]
    public async Task GetCurrentUser_ValidUserId_ReturnsOkWithCurrentUserResponse()
    {
        // Arrange
        var userId = 1;
        var currentUserResponse = new CurrentUserResponse
        {
            User = new UserDto
            {
                Id = userId,
                Username = "testuser",
                Email = "test@example.com"
            }
        };

        _mockAuthService.Setup(x => x.GetCurrentUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentUserResponse);

        SetupAuthenticatedUser(userId);

        // Act
        var result = await Controller.GetCurrentUser(CancellationToken.None);

        // Assert
        var response = ControllerAssertionUtilities.AssertOkResult<CurrentUserResponse>(result);
        response.User.Id.Should().Be(currentUserResponse.User.Id);
        response.User.Username.Should().Be(currentUserResponse.User.Username);
        response.User.Email.Should().Be(currentUserResponse.User.Email);
    }

    [Fact]
    public async Task GetCurrentUser_MissingUserIdClaim_ReturnsUnauthorized()
    {
        // Arrange - Controller already has no user set up

        // Act
        var result = await Controller.GetCurrentUser(CancellationToken.None);

        // Assert
        ControllerAssertionUtilities.AssertUnauthorizedResult(result, "Invalid token.");
    }

    [Fact]
    public async Task GetCurrentUser_InvalidUserIdClaim_ReturnsUnauthorized()
    {
        // Arrange
        // Set up invalid user ID claim
        if (Controller is ControllerBase controllerBase)
        {
            var user = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[]
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, "invalid-id")
            }));
            controllerBase.ControllerContext = new ControllerContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
                {
                    User = user
                }
            };
        }

        // Act
        var result = await Controller.GetCurrentUser(CancellationToken.None);

        // Assert
        ControllerAssertionUtilities.AssertUnauthorizedResult(result, "Invalid token.");
    }

    [Fact]
    public async Task GetCurrentUser_UserNotFound_ReturnsUnauthorized()
    {
        // Arrange
        var userId = 999;

        _mockAuthService.Setup(x => x.GetCurrentUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CurrentUserResponse?)null);

        SetupAuthenticatedUser(userId);

        // Act
        var result = await Controller.GetCurrentUser(CancellationToken.None);

        // Assert
        ControllerAssertionUtilities.AssertUnauthorizedResult(result, "User not found.");
    }

    [Fact]
    public async Task Register_NullTokenInResult_ReturnsEmptyStringInResponse()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "Password123"
        };

        var authResult = new AuthResult
        {
            Success = true,
            Token = null,
            UserId = 1,
            Username = "testuser"
        };

        _mockAuthService.Setup(x => x.RegisterAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(authResult);

        // Act
        var result = await Controller.Register(request, CancellationToken.None);

        // Assert
        var response = ControllerAssertionUtilities.AssertOkResult<AuthSuccessResponse>(result);
        response.Token.Should().BeEmpty();
        response.UserId.Should().Be(authResult.UserId);
        response.Username.Should().Be(authResult.Username);
    }

    [Fact]
    public async Task Login_NullUsernameInResult_ReturnsEmptyStringInResponse()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = "testuser",
            Password = "Password123"
        };

        var authResult = new AuthResult
        {
            Success = true,
            Token = "test-token",
            UserId = 1,
            Username = null
        };

        _mockAuthService.Setup(x => x.LoginAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(authResult);

        // Act
        var result = await Controller.Login(request, CancellationToken.None);

        // Assert
        var response = ControllerAssertionUtilities.AssertOkResult<AuthSuccessResponse>(result);
        response.Token.Should().Be(authResult.Token);
        response.UserId.Should().Be(authResult.UserId);
        response.Username.Should().BeEmpty();
    }
}
