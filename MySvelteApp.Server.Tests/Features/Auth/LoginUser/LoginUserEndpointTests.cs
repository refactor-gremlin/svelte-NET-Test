using Microsoft.AspNetCore.Mvc;
using Moq;
using FluentAssertions;
using MySvelteApp.Server.Features.Auth.LoginUser;
using MySvelteApp.Server.Shared.Common.DTOs;
using MySvelteApp.Server.Shared.Common.DTOs.Responses;
using MySvelteApp.Server.Shared.Common.Results;
using MySvelteApp.Server.Tests.TestFixtures;

namespace MySvelteApp.Server.Tests.Features.Auth.LoginUser;

public class LoginUserEndpointTests : ControllerTestTemplate<LoginUserEndpoint>
{
    private readonly Mock<LoginUserHandler> _mockHandler = new();

    protected override LoginUserEndpoint CreateController()
    {
        return new LoginUserEndpoint(_mockHandler.Object);
    }

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsOkWithLoginUserResponse()
    {
        // Arrange
        var request = GenericTestDataFactory.CreateLoginRequest();
        var response = new LoginUserResponse
        {
            Token = "test-token",
            User = new UserDto
            {
                Id = 1,
                Username = "testuser",
                Email = "test@example.com"
            }
        };

        _mockHandler.Setup(x => x.HandleAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<LoginUserResponse>.Success(response));

        // Act
        var result = await Controller.Handle(request, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<LoginUserResponse>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Token.Should().Be(response.Token);
        apiResponse.Data.User.Id.Should().Be(response.User.Id);
        apiResponse.Data.User.Username.Should().Be(response.User.Username);
        apiResponse.Data.User.Email.Should().Be(response.User.Email);
    }

    [Fact]
    public async Task Handle_UnauthorizedError_ReturnsUnauthorized()
    {
        // Arrange
        var request = GenericTestDataFactory.CreateLoginRequest();

        _mockHandler.Setup(x => x.HandleAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<LoginUserResponse>.Unauthorized("Invalid username or password"));

        // Act
        var result = await Controller.Handle(request, CancellationToken.None);

        // Assert
        var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        var errorResponse = unauthorizedResult.Value.Should().BeOfType<ApiErrorResponse>().Subject;
        errorResponse.Message.Should().Contain("Invalid username or password");
    }
}

