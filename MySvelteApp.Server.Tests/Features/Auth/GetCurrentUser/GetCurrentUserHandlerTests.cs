using Moq;
using FluentAssertions;
using MySvelteApp.Server.Features.Auth.GetCurrentUser;
using MySvelteApp.Server.Shared.Common.Interfaces;
using MySvelteApp.Server.Shared.Common.Results;
using MySvelteApp.Server.Shared.Domain.Entities;
using MySvelteApp.Server.Tests.TestFixtures;

namespace MySvelteApp.Server.Tests.Features.Auth.GetCurrentUser;

public class GetCurrentUserHandlerTests
{
    private readonly Mock<IUserRepository> _mockUserRepository = new();
    private readonly GetCurrentUserHandler _handler;

    public GetCurrentUserHandlerTests()
    {
        _handler = new GetCurrentUserHandler(_mockUserRepository.Object);
    }

    [Fact]
    public async Task HandleAsync_ValidUserId_ReturnsCurrentUserResponse()
    {
        // Arrange
        var userId = 1;
        var user = GenericTestDataFactory.CreateUser(userId);

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.HandleAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.User.Id.Should().Be(user.Id);
        result.Value.User.Username.Should().Be(user.Username);
        result.Value.User.Email.Should().Be(user.Email);
    }

    [Fact]
    public async Task HandleAsync_NonExistentUserId_ReturnsNotFound()
    {
        // Arrange
        var userId = 999;

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _handler.HandleAsync(userId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ApiErrorType.NotFound);
        result.ErrorMessage.Should().Contain("User not found");
    }
}

