using FluentAssertions;
using MySvelteApp.Server.Shared.Domain.ValueObjects;

namespace MySvelteApp.Server.Tests.Shared.Domain.ValueObjects;

public class EmailTests
{
    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user.name@example.co.uk")]
    [InlineData("user+tag@example.com")]
    public void Create_ValidEmail_ReturnsEmail(string emailInput)
    {
        // Act
        var email = Email.Create(emailInput);

        // Assert
        email.Should().NotBeNull();
        email.Value.Should().Be(emailInput.ToLowerInvariant().Trim());
    }

    [Fact]
    public void Create_EmailWithWhitespace_NormalizesEmail()
    {
        // Arrange
        var input = "  Test@Example.COM  ";

        // Act
        var email = Email.Create(input);

        // Assert
        email.Value.Should().Be("test@example.com");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_EmptyOrNullEmail_ThrowsArgumentException(string? emailInput)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => Email.Create(emailInput!));
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("@example.com")]
    [InlineData("user@")]
    [InlineData("user@example")]
    public void Create_InvalidEmailFormat_ThrowsArgumentException(string emailInput)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => Email.Create(emailInput));
    }

    [Fact]
    public void TryCreate_ValidEmail_ReturnsEmail()
    {
        // Act
        var email = Email.TryCreate("test@example.com");

        // Assert
        email.Should().NotBeNull();
        email!.Value.Should().Be("test@example.com");
    }

    [Fact]
    public void TryCreate_InvalidEmail_ReturnsNull()
    {
        // Act
        var email = Email.TryCreate("invalid-email");

        // Assert
        email.Should().BeNull();
    }

    [Fact]
    public void Equals_SameEmail_ReturnsTrue()
    {
        // Arrange
        var email1 = Email.Create("test@example.com");
        var email2 = Email.Create("TEST@EXAMPLE.COM");

        // Act & Assert
        email1.Should().Be(email2);
        email1.Equals(email2).Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentEmails_ReturnsFalse()
    {
        // Arrange
        var email1 = Email.Create("test1@example.com");
        var email2 = Email.Create("test2@example.com");

        // Act & Assert
        email1.Should().NotBe(email2);
    }

    [Fact]
    public void ToString_ReturnsEmailValue()
    {
        // Arrange
        var email = Email.Create("test@example.com");

        // Act & Assert
        email.ToString().Should().Be("test@example.com");
    }

    [Fact]
    public void ImplicitConversion_ToString_Works()
    {
        // Arrange
        var email = Email.Create("test@example.com");

        // Act
        string emailString = email;

        // Assert
        emailString.Should().Be("test@example.com");
    }
}
