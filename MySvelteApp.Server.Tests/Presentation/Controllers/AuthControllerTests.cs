using Microsoft.AspNetCore.Mvc;
using Moq;
using FluentAssertions;
using MySvelteApp.Server.Presentation.Controllers;
using MySvelteApp.Server.Application.Authentication;
using MySvelteApp.Server.Application.Authentication.DTOs;
using MySvelteApp.Server.Presentation.Models.Auth;

namespace MySvelteApp.Server.Tests.Presentation.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _mockAuthService;
    private readonly AuthController _authController;

    public AuthControllerTests()
    {
        _mockAuthService = new Mock<IAuthService>();
        _authController = new AuthController(_mockAuthService.Object);
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
        var result = await _authController.Register(request, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<AuthSuccessResponse>().Subject;

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
        var result = await _authController.Register(request, CancellationToken.None);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var response = badRequestResult.Value.Should().BeOfType<AuthErrorResponse>().Subject;

        response.Message.Should().Be(authResult.ErrorMessage);
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
        var result = await _authController.Register(request, CancellationToken.None);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var response = badRequestResult.Value.Should().BeOfType<AuthErrorResponse>().Subject;

        response.Message.Should().Be(authResult.ErrorMessage);
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
        var result = await _authController.Login(request, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<AuthSuccessResponse>().Subject;

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
        var result = await _authController.Login(request, CancellationToken.None);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var response = badRequestResult.Value.Should().BeOfType<AuthErrorResponse>().Subject;

        response.Message.Should().Be(authResult.ErrorMessage);
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
        var result = await _authController.Login(request, CancellationToken.None);

        // Assert
        var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        var response = unauthorizedResult.Value.Should().BeOfType<AuthErrorResponse>().Subject;

        response.Message.Should().Be(authResult.ErrorMessage);
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

        // Create a controller with a mock HttpContext that contains the user claim
        var controller = new AuthController(_mockAuthService.Object);
        var user = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[]
        {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userId.ToString())
        }));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
            {
                User = user
            }
        };

        // Act
        var result = await controller.GetCurrentUser(CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<CurrentUserResponse>().Subject;

        response.User.Id.Should().Be(currentUserResponse.User.Id);
        response.User.Username.Should().Be(currentUserResponse.User.Username);
        response.User.Email.Should().Be(currentUserResponse.User.Email);
    }

    [Fact]
    public async Task GetCurrentUser_MissingUserIdClaim_ReturnsUnauthorized()
    {
        // Arrange
        var controller = new AuthController(_mockAuthService.Object);
        var user = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity()); // No claims
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
            {
                User = user
            }
        };

        // Act
        var result = await controller.GetCurrentUser(CancellationToken.None);

        // Assert
        var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        var response = unauthorizedResult.Value.Should().BeOfType<AuthErrorResponse>().Subject;

        response.Message.Should().Be("Invalid token.");
    }

    [Fact]
    public async Task GetCurrentUser_InvalidUserIdClaim_ReturnsUnauthorized()
    {
        // Arrange
        var controller = new AuthController(_mockAuthService.Object);
        var user = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[]
        {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, "invalid-id")
        }));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
            {
                User = user
            }
        };

        // Act
        var result = await controller.GetCurrentUser(CancellationToken.None);

        // Assert
        var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        var response = unauthorizedResult.Value.Should().BeOfType<AuthErrorResponse>().Subject;

        response.Message.Should().Be("Invalid token.");
    }

    [Fact]
    public async Task GetCurrentUser_UserNotFound_ReturnsUnauthorized()
    {
        // Arrange
        var userId = 999;

        _mockAuthService.Setup(x => x.GetCurrentUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CurrentUserResponse?)null);

        var controller = new AuthController(_mockAuthService.Object);
        var user = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[]
        {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userId.ToString())
        }));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
            {
                User = user
            }
        };

        // Act
        var result = await controller.GetCurrentUser(CancellationToken.None);

        // Assert
        var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        var response = unauthorizedResult.Value.Should().BeOfType<AuthErrorResponse>().Subject;

        response.Message.Should().Be("User not found.");
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
        var result = await _authController.Register(request, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<AuthSuccessResponse>().Subject;

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
        var result = await _authController.Login(request, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<AuthSuccessResponse>().Subject;

        response.Token.Should().Be(authResult.Token);
        response.UserId.Should().Be(authResult.UserId);
        response.Username.Should().BeEmpty();
    }
}
