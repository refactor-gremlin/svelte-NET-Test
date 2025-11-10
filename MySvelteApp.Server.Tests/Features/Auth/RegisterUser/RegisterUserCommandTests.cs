using Moq;
using FluentAssertions;
using MySvelteApp.Server.Features.Auth.RegisterUser;
using MySvelteApp.Server.Shared.Common.Interfaces;
using MySvelteApp.Server.Shared.Common.Results;
using MySvelteApp.Server.Shared.Domain.Entities;
using MySvelteApp.Server.Shared.Domain.Events;
using MySvelteApp.Server.Shared.Domain.Services;
using MySvelteApp.Server.Shared.Domain.ValueObjects;
using MySvelteApp.Server.Tests.TestFixtures;

namespace MySvelteApp.Server.Tests.Features.Auth.RegisterUser;

public class RegisterUserCommandTests
{
    private readonly Mock<IUserDomainService> _mockUserDomainService = new();
    private readonly Mock<IPasswordHasher> _mockPasswordHasher = new();
    private readonly Mock<IJwtTokenGenerator> _mockJwtTokenGenerator = new();
    private readonly Mock<IUserRepository> _mockUserRepository = new();
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly Mock<IDomainEventPublisher> _mockEventPublisher = new();
    private readonly RegisterUserCommand _handler;

    public RegisterUserCommandTests()
    {
        _handler = new RegisterUserCommand(
            _mockUserDomainService.Object,
            _mockPasswordHasher.Object,
            _mockJwtTokenGenerator.Object,
            _mockUserRepository.Object,
            _mockUnitOfWork.Object,
            _mockEventPublisher.Object);
    }

    [Fact]
    public async Task HandleAsync_ValidRequest_ReturnsSuccessResult()
    {
        // Arrange
        var request = GenericTestDataFactory.CreateRegisterRequest();
        var username = Username.Create(request.Username);
        var email = Email.Create(request.Email);
        var user = new User
        {
            Id = 1,
            Username = username,
            Email = email,
            PasswordHash = "testhash",
            PasswordSalt = "testsalt"
        };
        var token = "test-token";

        _mockUserDomainService.Setup(x => x.CanRegisterUserAsync(
                It.IsAny<Username>(), 
                It.IsAny<Email>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, (string?)null));
        _mockPasswordHasher.Setup(x => x.HashPassword(request.Password))
            .Returns(("testhash", "testsalt"));
        _mockUserDomainService.Setup(x => x.CreateUser(
                It.IsAny<Username>(),
                It.IsAny<Email>(),
                "testhash",
                "testsalt"))
            .Returns(user);
        _mockUserRepository.Setup(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _mockJwtTokenGenerator.Setup(x => x.GenerateToken(It.IsAny<User>()))
            .Returns(token);
        _mockEventPublisher.Setup(x => x.PublishAsync(
                It.IsAny<UserRegisteredEvent>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Token.Should().Be(token);
        result.Value.User.Should().NotBeNull();
        result.Value.User.Id.Should().Be(user.Id);
        result.Value.User.Username.Should().Be(username.Value);
        result.Value.User.Email.Should().Be(email.Value);
        _mockEventPublisher.Verify(x => x.PublishAsync(
            It.IsAny<UserRegisteredEvent>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_InvalidUsername_ReturnsValidationError()
    {
        // Arrange
        var request = GenericTestDataFactory.CreateRegisterRequest();
        request.Username = "ab"; // Too short

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ApiErrorType.Validation);
    }

    [Fact]
    public async Task HandleAsync_InvalidEmail_ReturnsValidationError()
    {
        // Arrange
        var request = GenericTestDataFactory.CreateRegisterRequest();
        request.Email = "invalid-email"; // Invalid format

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ApiErrorType.Validation);
    }

    [Fact]
    public async Task HandleAsync_ExistingUsername_ReturnsConflictError()
    {
        // Arrange
        var request = GenericTestDataFactory.CreateRegisterRequest();
        var username = Username.Create(request.Username);
        var email = Email.Create(request.Email);

        _mockUserDomainService.Setup(x => x.CanRegisterUserAsync(
                It.IsAny<Username>(), 
                It.IsAny<Email>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "This username is already taken. Please choose a different one."));

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
        var username = Username.Create(request.Username);
        var email = Email.Create(request.Email);

        _mockUserDomainService.Setup(x => x.CanRegisterUserAsync(
                It.IsAny<Username>(), 
                It.IsAny<Email>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "This email is already registered. Please use a different email address."));

        // Act
        var result = await _handler.HandleAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ApiErrorType.Conflict);
        result.ErrorMessage.Should().Contain("email is already registered");
    }
}

