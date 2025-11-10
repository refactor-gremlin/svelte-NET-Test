using Moq;
using FluentAssertions;
using MySvelteApp.Server.Features.Auth.RegisterUser;
using MySvelteApp.Server.Shared.Common.Interfaces;
using MySvelteApp.Server.Shared.Common.Results;
using MySvelteApp.Server.Shared.Domain.Entities;
using MySvelteApp.Server.Tests.TestFixtures;

namespace MySvelteApp.Server.Tests.Features.Auth.RegisterUser;

public class RegisterUserHandlerTests
{
    private readonly Mock<IUserRepository> _mockUserRepository = new();
    private readonly Mock<IPasswordHasher> _mockPasswordHasher = new();
    private readonly Mock<IJwtTokenGenerator> _mockJwtTokenGenerator = new();
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly RegisterUserHandler _handler;

    public RegisterUserHandlerTests()
    {
        _handler = new RegisterUserHandler(
            _mockUserRepository.Object,
            _mockPasswordHasher.Object,
            _mockJwtTokenGenerator.Object,
            _mockUnitOfWork.Object);
    }

    [Fact]
    public async Task HandleAsync_ValidRequest_ReturnsSuccessResult()
    {
        // Arrange
        var request = GenericTestDataFactory.CreateRegisterRequest();
        var user = GenericTestDataFactory.CreateUser();
        var token = "test-token";

        _mockUserRepository.Setup(x => x.UsernameExistsAsync(request.Username.Trim(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockUserRepository.Setup(x => x.EmailExistsAsync(request.Email.Trim().ToLowerInvariant(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockPasswordHasher.Setup(x => x.HashPassword(request.Password))
            .Returns(("testhash", "testsalt"));
        _mockUserRepository.Setup(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mockJwtTokenGenerator.Setup(x => x.GenerateToken(It.IsAny<User>()))
            .Returns(token);

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Token.Should().Be(token);
        result.Value.UserId.Should().BeGreaterThanOrEqualTo(0);
        result.Value.Username.Should().Be(request.Username.Trim());
    }

    [Fact]
    public async Task HandleAsync_ExistingUsername_ReturnsConflictError()
    {
        // Arrange
        var request = GenericTestDataFactory.CreateRegisterRequest();

        _mockUserRepository.Setup(x => x.UsernameExistsAsync(request.Username.Trim(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ApiErrorType.Conflict);
        result.ErrorMessage.Should().Contain("username is already taken");
    }

    [Fact]
    public async Task HandleAsync_ExistingEmail_ReturnsConflictError()
    {
        // Arrange
        var request = GenericTestDataFactory.CreateRegisterRequest();

        _mockUserRepository.Setup(x => x.UsernameExistsAsync(request.Username.Trim(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockUserRepository.Setup(x => x.EmailExistsAsync(request.Email.Trim().ToLowerInvariant(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ApiErrorType.Conflict);
        result.ErrorMessage.Should().Contain("email is already registered");
    }
}

