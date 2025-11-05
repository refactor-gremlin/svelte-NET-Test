using FluentAssertions;
using MySvelteApp.Server.Infrastructure.Security;

namespace MySvelteApp.Server.Tests.Infrastructure.Security;

public class PasswordHasherTests
{
    private readonly PasswordHasher _passwordHasher;

    public PasswordHasherTests()
    {
        _passwordHasher = new PasswordHasher();
    }

    [Fact]
    public void HashPassword_ValidPassword_ReturnsHashAndSalt()
    {
        // Arrange
        var password = "TestPassword123";

        // Act
        var result = _passwordHasher.HashPassword(password);

        // Assert
        result.Hash.Should().NotBeNullOrEmpty();
        result.Salt.Should().NotBeNullOrEmpty();
        result.Hash.Should().NotBe(password);
        result.Salt.Should().NotBe(password);
    }

    [Fact]
    public void HashPassword_SamePasswordMultipleTimes_ReturnsDifferentSalts()
    {
        // Arrange
        var password = "TestPassword123";

        // Act
        var result1 = _passwordHasher.HashPassword(password);
        var result2 = _passwordHasher.HashPassword(password);

        // Assert
        result1.Hash.Should().NotBe(result2.Hash);
        result1.Salt.Should().NotBe(result2.Salt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("short")]
    [InlineData("verylongpassword123456789!@#$%^&*()")]
    [InlineData("Password with spaces")]
    [InlineData("P@ssw0rd!#$%^&*()")]
    [InlineData("😀🔒🔑")] // Unicode characters
    public void HashPassword_VariousPasswords_ReturnsValidHashAndSalt(string password)
    {
        // Act
        var result = _passwordHasher.HashPassword(password);

        // Assert
        result.Hash.Should().NotBeNullOrEmpty();
        result.Salt.Should().NotBeNullOrEmpty();
        result.Hash.Should().NotBe(password);
        result.Salt.Should().NotBe(password);
    }

    [Fact]
    public void VerifyPassword_CorrectPassword_ReturnsTrue()
    {
        // Arrange
        var password = "TestPassword123";
        var hashResult = _passwordHasher.HashPassword(password);

        // Act
        var result = _passwordHasher.VerifyPassword(password, hashResult.Hash, hashResult.Salt);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_IncorrectPassword_ReturnsFalse()
    {
        // Arrange
        var password = "TestPassword123";
        var wrongPassword = "WrongPassword456";
        var hashResult = _passwordHasher.HashPassword(password);

        // Act
        var result = _passwordHasher.VerifyPassword(wrongPassword, hashResult.Hash, hashResult.Salt);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_DifferentSalt_ReturnsFalse()
    {
        // Arrange
        var password = "TestPassword123";
        var hashResult1 = _passwordHasher.HashPassword(password);
        var hashResult2 = _passwordHasher.HashPassword(password);

        // Act
        var result = _passwordHasher.VerifyPassword(password, hashResult1.Hash, hashResult2.Salt);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_DifferentHash_ReturnsFalse()
    {
        // Arrange
        var password = "TestPassword123";
        var hashResult1 = _passwordHasher.HashPassword(password);
        var hashResult2 = _passwordHasher.HashPassword(password);

        // Act
        var result = _passwordHasher.VerifyPassword(password, hashResult2.Hash, hashResult1.Salt);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("short")]
    [InlineData("verylongpassword123456789!@#$%^&*()")]
    [InlineData("Password with spaces")]
    [InlineData("P@ssw0rd!#$%^&*()")]
    [InlineData("😀🔒🔑")] // Unicode characters
    public void VerifyPassword_VariousValidPasswords_ReturnsTrue(string password)
    {
        // Arrange
        var hashResult = _passwordHasher.HashPassword(password);

        // Act
        var result = _passwordHasher.VerifyPassword(password, hashResult.Hash, hashResult.Salt);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("invalid base64 hash")]
    [InlineData("invalid base64 salt")]
    [InlineData("YWJjZGVm")] // Short base64
    [InlineData("YWJjZGVmZ2hpamtsbW5vcHFyc3R1dnd4eXoxMjM0NTY3ODkwYWJjZGVmZ2hpamtsbW5vcHFyc3R1dnd4eXoxMjM0NTY3ODkwYWJjZGVmZ2hpamtsbW5vcHFyc3R1dnd4eXoxMjM0NTY3ODkw")] // Very long base64
    public void VerifyPassword_InvalidHashOrSalt_ReturnsFalse(string invalidInput)
    {
        // Arrange
        var password = "TestPassword123";
        var validHashResult = _passwordHasher.HashPassword(password);

        // Act & Assert - Test invalid hash
        var result1 = _passwordHasher.VerifyPassword(password, invalidInput, validHashResult.Salt);
        result1.Should().BeFalse();

        // Act & Assert - Test invalid salt
        var result2 = _passwordHasher.VerifyPassword(password, validHashResult.Hash, invalidInput);
        result2.Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_NullHashOrSalt_ReturnsFalse()
    {
        // Arrange
        var password = "TestPassword123";
        var validHashResult = _passwordHasher.HashPassword(password);

        // Act & Assert - Test null hash
        Action act1 = () => _passwordHasher.VerifyPassword(password, null!, validHashResult.Salt);
        act1.Should().Throw<ArgumentNullException>();

        // Act & Assert - Test null salt
        Action act2 = () => _passwordHasher.VerifyPassword(password, validHashResult.Hash, null!);
        act2.Should().Throw<ArgumentNullException>();
    }
}
