using Microsoft.AspNetCore.Mvc;
using Moq;
using FluentAssertions;
using MySvelteApp.Server.Features.Auth.GetCurrentUser;
using MySvelteApp.Server.Shared.Common.DTOs;
using MySvelteApp.Server.Shared.Common.DTOs.Responses;
using MySvelteApp.Server.Shared.Common.Results;
using MySvelteApp.Server.Tests.TestFixtures;

namespace MySvelteApp.Server.Tests.Features.Auth.GetCurrentUser;

public class GetCurrentUserEndpointTests : ControllerTestTemplate<GetCurrentUserEndpoint>
{
    private readonly Mock<GetCurrentUserQuery> _mockHandler = new();

    protected override GetCurrentUserEndpoint CreateController()
    {
        return new GetCurrentUserEndpoint(_mockHandler.Object);
    }

    [Fact]
    public async Task Handle_ValidUserId_ReturnsOkWithGetCurrentUserResponse()
    {
        // Arrange
        var userId = 1;
        var response = new GetCurrentUserResponse
        {
            User = new UserDto
            {
                Id = userId,
                Username = "testuser",
                Email = "test@example.com"
            }
        };

        _mockHandler.Setup(x => x.HandleAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<GetCurrentUserResponse>.Success(response));

        SetupAuthenticatedUser(userId);

        // Act
        var result = await Controller.Handle(CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<GetCurrentUserResponse>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.User.Id.Should().Be(response.User.Id);
        apiResponse.Data.User.Username.Should().Be(response.User.Username);
        apiResponse.Data.User.Email.Should().Be(response.User.Email);
    }

    [Fact]
    public async Task Handle_MissingUserIdClaim_ReturnsUnauthorized()
    {
        // Arrange - Controller already has no user set up

        // Act
        var result = await Controller.Handle(CancellationToken.None);

        // Assert
        var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        var errorResponse = unauthorizedResult.Value.Should().BeOfType<ApiErrorResponse>().Subject;
        errorResponse.Message.Should().Contain("Invalid token");
    }

    [Fact]
    public async Task Handle_InvalidUserIdClaim_ReturnsUnauthorized()
    {
        // Arrange
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
        var result = await Controller.Handle(CancellationToken.None);

        // Assert
        var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        var errorResponse = unauthorizedResult.Value.Should().BeOfType<ApiErrorResponse>().Subject;
        errorResponse.Message.Should().Contain("Invalid token");
    }

    [Fact]
    public async Task Handle_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        var userId = 999;

        _mockHandler.Setup(x => x.HandleAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<GetCurrentUserResponse>.NotFound("User not found"));

        SetupAuthenticatedUser(userId);

        // Act
        var result = await Controller.Handle(CancellationToken.None);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var errorResponse = notFoundResult.Value.Should().BeOfType<ApiErrorResponse>().Subject;
        errorResponse.Message.Should().Contain("User not found");
    }
}

