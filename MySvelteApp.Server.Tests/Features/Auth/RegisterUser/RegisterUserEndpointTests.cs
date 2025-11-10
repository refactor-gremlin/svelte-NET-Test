using Microsoft.AspNetCore.Mvc;
using Moq;
using FluentAssertions;
using MySvelteApp.Server.Features.Auth.RegisterUser;
using MySvelteApp.Server.Shared.Common.DTOs;
using MySvelteApp.Server.Shared.Common.DTOs.Responses;
using MySvelteApp.Server.Shared.Common.Results;
using MySvelteApp.Server.Tests.TestFixtures;

namespace MySvelteApp.Server.Tests.Features.Auth.RegisterUser;

public class RegisterUserEndpointTests : ControllerTestTemplate<RegisterUserEndpoint>
{
    private readonly Mock<RegisterUserHandler> _mockHandler = new();

    protected override RegisterUserEndpoint CreateController()
    {
        return new RegisterUserEndpoint(_mockHandler.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_ReturnsOkWithRegisterUserResponse()
    {
        // Arrange
        var request = GenericTestDataFactory.CreateRegisterRequest();
        var response = new RegisterUserResponse
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
            .ReturnsAsync(ApiResult<RegisterUserResponse>.Success(response));

        // Act
        var result = await Controller.Handle(request, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<RegisterUserResponse>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data.Token.Should().Be(response.Token);
        apiResponse.Data.User.Id.Should().Be(response.User.Id);
        apiResponse.Data.User.Username.Should().Be(response.User.Username);
        apiResponse.Data.User.Email.Should().Be(response.User.Email);
    }

    [Fact]
    public async Task Handle_ConflictError_ReturnsConflict()
    {
        // Arrange
        var request = GenericTestDataFactory.CreateRegisterRequest();

        _mockHandler.Setup(x => x.HandleAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<RegisterUserResponse>.Conflict("Username already taken"));

        // Act
        var result = await Controller.Handle(request, CancellationToken.None);

        // Assert
        var conflictResult = result.Should().BeOfType<ConflictObjectResult>().Subject;
        var errorResponse = conflictResult.Value.Should().BeOfType<ApiErrorResponse>().Subject;
        errorResponse.Message.Should().Contain("Username already taken");
    }
}

