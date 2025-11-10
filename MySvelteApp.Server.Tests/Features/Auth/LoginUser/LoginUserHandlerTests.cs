using Moq;
using FluentAssertions;
using MySvelteApp.Server.Features.Auth.LoginUser;
using MySvelteApp.Server.Shared.Common.Interfaces;
using MySvelteApp.Server.Shared.Common.Results;
using MySvelteApp.Server.Shared.Domain.Entities;
using MySvelteApp.Server.Shared.Domain.ValueObjects;
using MySvelteApp.Server.Tests.TestFixtures;

namespace MySvelteApp.Server.Tests.Features.Auth.LoginUser;

public class LoginUserHandlerTests
{
    private readonly Mock<IUserRepository> _mockUserRepository = new();
    private readonly Mock<IPasswordHasher> _mockPasswordHasher = new();
    private readonly Mock<IJwtTokenGenerator> _mockJwtTokenGenerator = new();
    private readonly LoginUserHandler _handler;

    public LoginUserHandlerTests()
    {
        _handler = new LoginUserHandler(
            _mockUserRepository.Object,
            _mockPasswordHasher.Object,
            _mockJwtTokenGenerator.Object);
    }

    [Fact]
    public async Task HandleAsync_ValidCredentials_ReturnsSuccessResult()
    {
        // Arrange
        var request = GenericTestDataFactory.CreateLoginRequest();
        var user = GenericTestDataFactory.CreateUser();
        var token = "test-token";

        _mockUserRepository.Setup(x => x.GetByUsernameAsync(It.IsAny<Username>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockPasswordHasher.Setup(x => x.VerifyPassword(request.Password, user.PasswordHash, user.PasswordSalt))
            .Returns(true);
        _mockJwtTokenGenerator.Setup(x => x.GenerateToken(user))
            .Returns(token);

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Token.Should().Be(token);
        result.Value.User.Should().NotBeNull();
        result.Value.User.Id.Should().BeGreaterThanOrEqualTo(0);
        result.Value.User.Username.Should().Be(user.Username.Value);
        result.Value.User.Email.Should().Be(user.Email.Value);
    }

    [Fact]
    public async Task HandleAsync_NonExistentUser_ReturnsUnauthorizedError()
    {
        // Arrange
        var request = GenericTestDataFactory.CreateLoginRequest();

        _mockUserRepository.Setup(x => x.GetByUsernameAsync(It.IsAny<Username>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ApiErrorType.Unauthorized);
        result.ErrorMessage.Should().Contain("Invalid username or password");
    }

    [Fact]
    public async Task HandleAsync_InvalidPassword_ReturnsUnauthorizedError()
    {
        // Arrange
        var request = GenericTestDataFactory.CreateLoginRequest();
        var user = GenericTestDataFactory.CreateUser();

        _mockUserRepository.Setup(x => x.GetByUsernameAsync(It.IsAny<Username>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockPasswordHasher.Setup(x => x.VerifyPassword(request.Password, user.PasswordHash, user.PasswordSalt))
            .Returns(false);

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ApiErrorType.Unauthorized);
        result.ErrorMessage.Should().Contain("Invalid username or password");
    }
}

