using Microsoft.Extensions.Configuration;
using Moq;
using FluentAssertions;
using MySvelteApp.Server.Application.Authentication;
using MySvelteApp.Server.Application.Authentication.DTOs;
using MySvelteApp.Server.Application.Common.Interfaces;
using MySvelteApp.Server.Domain.Entities;
using MySvelteApp.Server.Tests.TestFixtures;

namespace MySvelteApp.Server.Tests.Application.Authentication;

public class AuthServiceTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IPasswordHasher> _mockPasswordHasher;
    private readonly Mock<IJwtTokenGenerator> _mockJwtTokenGenerator;
    private readonly AuthService _authService;

    public AuthServiceTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _mockUserRepository = new Mock<IUserRepository>();
        _mockPasswordHasher = new Mock<IPasswordHasher>();
        _mockJwtTokenGenerator = new Mock<IJwtTokenGenerator>();

        _authService = new AuthService(
            _mockUserRepository.Object,
            _mockPasswordHasher.Object,
            _mockJwtTokenGenerator.Object);
    }

    [Fact]
    public async Task RegisterAsync_ValidRequest_ReturnsSuccessResult()
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
        _mockJwtTokenGenerator.Setup(x => x.GenerateToken(It.IsAny<User>()))
            .Returns(token);

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Token.Should().Be(token);
        result.UserId.Should().BeGreaterThanOrEqualTo(0);
        result.Username.Should().Be(user.Username);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("ab")]
    [InlineData("invalid username!")]
    public async Task RegisterAsync_InvalidUsername_ReturnsValidationError(string username)
    {
        // Arrange
        var request = GenericTestDataFactory.CreateRegisterRequest(username: username);

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorType.Should().Be(AuthErrorType.Validation);
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("invalid-email")]
    [InlineData("email@")]
    [InlineData("@example.com")]
    public async Task RegisterAsync_InvalidEmail_ReturnsValidationError(string email)
    {
        // Arrange
        var request = GenericTestDataFactory.CreateRegisterRequest(email: email);

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorType.Should().Be(AuthErrorType.Validation);
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("1234567")]
    [InlineData("password")]
    [InlineData("PASSWORD")]
    [InlineData("12345678")]
    public async Task RegisterAsync_InvalidPassword_ReturnsValidationError(string password)
    {
        // Arrange
        var request = GenericTestDataFactory.CreateRegisterRequest(password: password);

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorType.Should().Be(AuthErrorType.Validation);
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RegisterAsync_ExistingUsername_ReturnsConflictError()
    {
        // Arrange
        var request = GenericTestDataFactory.CreateRegisterRequest();

        _mockUserRepository.Setup(x => x.UsernameExistsAsync(request.Username.Trim(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorType.Should().Be(AuthErrorType.Conflict);
        result.ErrorMessage.Should().Contain("username is already taken");
    }

    [Fact]
    public async Task RegisterAsync_ExistingEmail_ReturnsConflictError()
    {
        // Arrange
        var request = GenericTestDataFactory.CreateRegisterRequest();

        _mockUserRepository.Setup(x => x.UsernameExistsAsync(request.Username.Trim(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockUserRepository.Setup(x => x.EmailExistsAsync(request.Email.Trim().ToLowerInvariant(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorType.Should().Be(AuthErrorType.Conflict);
        result.ErrorMessage.Should().Contain("email is already registered");
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsSuccessResult()
    {
        // Arrange
        var request = GenericTestDataFactory.CreateLoginRequest();
        var user = GenericTestDataFactory.CreateUser();
        var token = "test-token";

        _mockUserRepository.Setup(x => x.GetByUsernameAsync(request.Username.Trim(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockPasswordHasher.Setup(x => x.VerifyPassword(request.Password, user.PasswordHash, user.PasswordSalt))
            .Returns(true);
        _mockJwtTokenGenerator.Setup(x => x.GenerateToken(user))
            .Returns(token);

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Token.Should().Be(token);
        result.UserId.Should().BeGreaterThanOrEqualTo(0);
        result.Username.Should().Be(user.Username);
    }

    [Fact]
    public async Task LoginAsync_NonExistentUser_ReturnsUnauthorizedError()
    {
        // Arrange
        var request = GenericTestDataFactory.CreateLoginRequest();

        _mockUserRepository.Setup(x => x.GetByUsernameAsync(request.Username.Trim(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorType.Should().Be(AuthErrorType.Unauthorized);
        result.ErrorMessage.Should().Contain("Invalid username or password");
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_ReturnsUnauthorizedError()
    {
        // Arrange
        var request = GenericTestDataFactory.CreateLoginRequest();
        var user = GenericTestDataFactory.CreateUser();

        _mockUserRepository.Setup(x => x.GetByUsernameAsync(request.Username.Trim(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockPasswordHasher.Setup(x => x.VerifyPassword(request.Password, user.PasswordHash, user.PasswordSalt))
            .Returns(false);

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorType.Should().Be(AuthErrorType.Unauthorized);
        result.ErrorMessage.Should().Contain("Invalid username or password");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task LoginAsync_MissingUsername_ReturnsValidationError(string username)
    {
        // Arrange
        var request = GenericTestDataFactory.CreateLoginRequest(username: username);

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorType.Should().Be(AuthErrorType.Validation);
        result.ErrorMessage.Should().Contain("Username is required");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task LoginAsync_MissingPassword_ReturnsValidationError(string password)
    {
        // Arrange
        var request = GenericTestDataFactory.CreateLoginRequest(password: password);

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorType.Should().Be(AuthErrorType.Validation);
        result.ErrorMessage.Should().Contain("Password is required");
    }

    [Fact]
    public async Task GetCurrentUserAsync_ValidUserId_ReturnsCurrentUserResponse()
    {
        // Arrange
        var userId = 1;
        var user = GenericTestDataFactory.CreateUser(userId);

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _authService.GetCurrentUserAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result!.User.Id.Should().Be(user.Id);
        result.User.Username.Should().Be(user.Username);
        result.User.Email.Should().Be(user.Email);
    }

    [Fact]
    public async Task GetCurrentUserAsync_NonExistentUserId_ReturnsNull()
    {
        // Arrange
        var userId = 999;

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _authService.GetCurrentUserAsync(userId);

        // Assert
        result.Should().BeNull();
    }
}
