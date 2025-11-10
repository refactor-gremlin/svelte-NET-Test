using FluentAssertions;
using Moq;
using MySvelteApp.Server.Shared.Common.Interfaces;
using MySvelteApp.Server.Shared.Domain.Entities;
using MySvelteApp.Server.Shared.Domain.Services;
using MySvelteApp.Server.Shared.Domain.ValueObjects;

namespace MySvelteApp.Server.Tests.Shared.Domain.Services;

public class UserDomainServiceTests
{
    private readonly Mock<IUserRepository> _mockRepository;
    private readonly UserDomainService _service;

    public UserDomainServiceTests()
    {
        _mockRepository = new Mock<IUserRepository>();
        _service = new UserDomainService(_mockRepository.Object);
    }

    [Fact]
    public async Task CanRegisterUserAsync_UsernameAndEmailAvailable_ReturnsTrue()
    {
        // Arrange
        var username = Username.Create("testuser");
        var email = Email.Create("test@example.com");

        _mockRepository.Setup(x => x.UsernameExistsAsync(username, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockRepository.Setup(x => x.EmailExistsAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var (canRegister, errorMessage) = await _service.CanRegisterUserAsync(username, email);

        // Assert
        canRegister.Should().BeTrue();
        errorMessage.Should().BeNull();
    }

    [Fact]
    public async Task CanRegisterUserAsync_UsernameExists_ReturnsFalse()
    {
        // Arrange
        var username = Username.Create("testuser");
        var email = Email.Create("test@example.com");

        _mockRepository.Setup(x => x.UsernameExistsAsync(username, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var (canRegister, errorMessage) = await _service.CanRegisterUserAsync(username, email);

        // Assert
        canRegister.Should().BeFalse();
        errorMessage.Should().Contain("username is already taken");
    }

    [Fact]
    public async Task CanRegisterUserAsync_EmailExists_ReturnsFalse()
    {
        // Arrange
        var username = Username.Create("testuser");
        var email = Email.Create("test@example.com");

        _mockRepository.Setup(x => x.UsernameExistsAsync(username, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockRepository.Setup(x => x.EmailExistsAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var (canRegister, errorMessage) = await _service.CanRegisterUserAsync(username, email);

        // Assert
        canRegister.Should().BeFalse();
        errorMessage.Should().Contain("email is already registered");
    }

    [Fact]
    public void CreateUser_ValidInputs_ReturnsUser()
    {
        // Arrange
        var username = Username.Create("testuser");
        var email = Email.Create("test@example.com");
        var passwordHash = "hashedpassword";
        var passwordSalt = "salt";

        // Act
        var user = _service.CreateUser(username, email, passwordHash, passwordSalt);

        // Assert
        user.Should().NotBeNull();
        user.Username.Should().Be(username);
        user.Email.Should().Be(email);
        user.PasswordHash.Should().Be(passwordHash);
        user.PasswordSalt.Should().Be(passwordSalt);
    }
}
