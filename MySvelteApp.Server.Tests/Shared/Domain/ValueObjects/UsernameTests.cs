using FluentAssertions;
using MySvelteApp.Server.Shared.Domain.ValueObjects;

namespace MySvelteApp.Server.Tests.Shared.Domain.ValueObjects;

public class UsernameTests
{
    [Theory]
    [InlineData("testuser")]
    [InlineData("user123")]
    [InlineData("user_name")]
    [InlineData("TestUser")]
    public void Create_ValidUsername_ReturnsUsername(string usernameInput)
    {
        // Act
        var username = Username.Create(usernameInput);

        // Assert
        username.Should().NotBeNull();
        username.Value.Should().Be(usernameInput.Trim());
    }

    [Fact]
    public void Create_UsernameWithWhitespace_TrimsWhitespace()
    {
        // Arrange
        var input = "  testuser  ";

        // Act
        var username = Username.Create(input);

        // Assert
        username.Value.Should().Be("testuser");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_EmptyOrNullUsername_ThrowsArgumentException(string? usernameInput)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => Username.Create(usernameInput!));
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("a")]
    public void Create_UsernameTooShort_ThrowsArgumentException(string usernameInput)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => Username.Create(usernameInput));
    }

    [Fact]
    public void Create_UsernameTooLong_ThrowsArgumentException()
    {
        // Arrange
        var longUsername = new string('a', 51);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => Username.Create(longUsername));
    }

    [Theory]
    [InlineData("user-name")]
    [InlineData("user.name")]
    [InlineData("user@name")]
    [InlineData("user name")]
    public void Create_UsernameWithInvalidCharacters_ThrowsArgumentException(string usernameInput)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => Username.Create(usernameInput));
    }

    [Fact]
    public void TryCreate_ValidUsername_ReturnsUsername()
    {
        // Act
        var username = Username.TryCreate("testuser");

        // Assert
        username.Should().NotBeNull();
        username!.Value.Should().Be("testuser");
    }

    [Fact]
    public void TryCreate_InvalidUsername_ReturnsNull()
    {
        // Act
        var username = Username.TryCreate("ab");

        // Assert
        username.Should().BeNull();
    }

    [Fact]
    public void Equals_SameUsername_ReturnsTrue()
    {
        // Arrange
        var username1 = Username.Create("testuser");
        var username2 = Username.Create("testuser");

        // Act & Assert
        username1.Should().Be(username2);
        username1.Equals(username2).Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentUsernames_ReturnsFalse()
    {
        // Arrange
        var username1 = Username.Create("user1");
        var username2 = Username.Create("user2");

        // Act & Assert
        username1.Should().NotBe(username2);
    }

    [Fact]
    public void ToString_ReturnsUsernameValue()
    {
        // Arrange
        var username = Username.Create("testuser");

        // Act & Assert
        username.ToString().Should().Be("testuser");
    }

    [Fact]
    public void ImplicitConversion_ToString_Works()
    {
        // Arrange
        var username = Username.Create("testuser");

        // Act
        string usernameString = username;

        // Assert
        usernameString.Should().Be("testuser");
    }
}
