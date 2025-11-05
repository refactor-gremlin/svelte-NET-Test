using Microsoft.Extensions.Configuration;
using Moq;
using FluentAssertions;
using MySvelteApp.Server.Infrastructure.Authentication;
using MySvelteApp.Server.Domain.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace MySvelteApp.Server.Tests.Infrastructure.Authentication;

public class JwtTokenGeneratorTests
{
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly JwtTokenGenerator _jwtTokenGenerator;

    public JwtTokenGeneratorTests()
    {
        _mockConfiguration = new Mock<IConfiguration>();
        
        // Setup default configuration values
        _mockConfiguration.Setup(x => x["Jwt:Key"]).Returns("this-is-a-test-secret-key-that-is-long-enough-for-hmac-sha256");
        _mockConfiguration.Setup(x => x["Jwt:Issuer"]).Returns("test-issuer");
        _mockConfiguration.Setup(x => x["Jwt:Audience"]).Returns("test-audience");

        _jwtTokenGenerator = new JwtTokenGenerator(_mockConfiguration.Object);
    }

    [Fact]
    public void GenerateToken_ValidUser_ReturnsJwtToken()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com"
        };

        // Act
        var token = _jwtTokenGenerator.GenerateToken(user);

        // Assert
        token.Should().NotBeNullOrEmpty();
        token.Should().Contain("eyJ"); // JWT tokens start with "eyJ" in base64
    }

    [Fact]
    public void GenerateToken_ValidUser_ContainsCorrectClaims()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com"
        };

        // Act
        var token = _jwtTokenGenerator.GenerateToken(user);

        // Assert
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);

        jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == user.Id.ToString());
        jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Name && c.Value == user.Username);
        jwtToken.Claims.Should().Contain(c => c.Type == "jti" && !string.IsNullOrEmpty(c.Value));
    }

    [Fact]
    public void GenerateToken_ValidUser_HasCorrectExpiration()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com"
        };

        // Act
        var token = _jwtTokenGenerator.GenerateToken(user);
        var beforeTime = DateTime.UtcNow;

        // Assert
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);

        var afterTime = DateTime.UtcNow;
        var expectedExpiration = beforeTime.AddHours(24);
        var actualExpiration = jwtToken.ValidTo;

        actualExpiration.Should().BeCloseTo(expectedExpiration, TimeSpan.FromMinutes(1));
        actualExpiration.Should().BeAfter(beforeTime);
        actualExpiration.Should().BeAfter(afterTime);
    }

    [Fact]
    public void GenerateToken_ValidUser_HasCorrectIssuerAndAudience()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com"
        };

        // Act
        var token = _jwtTokenGenerator.GenerateToken(user);

        // Assert
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);

        jwtToken.Issuer.Should().Be("test-issuer");
        jwtToken.Audiences.Should().Contain("test-audience");
    }

    [Fact]
    public void GenerateToken_DifferentUsers_ReturnsDifferentTokens()
    {
        // Arrange
        var user1 = new User
        {
            Id = 1,
            Username = "testuser1",
            Email = "test1@example.com"
        };
        var user2 = new User
        {
            Id = 2,
            Username = "testuser2",
            Email = "test2@example.com"
        };

        // Act
        var token1 = _jwtTokenGenerator.GenerateToken(user1);
        var token2 = _jwtTokenGenerator.GenerateToken(user2);

        // Assert
        token1.Should().NotBe(token2);
    }

    [Fact]
    public void GenerateToken_SameUser_ReturnsDifferentTokensEachTime()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com"
        };

        // Act
        var token1 = _jwtTokenGenerator.GenerateToken(user);
        var token2 = _jwtTokenGenerator.GenerateToken(user);

        // Assert
        token1.Should().NotBe(token2);
    }

    [Fact]
    public void GenerateToken_DefaultConfigurationValues_UsesFallbackValues()
    {
        // Arrange
        var mockConfigWithDefaults = new Mock<IConfiguration>();
        mockConfigWithDefaults.Setup(x => x["Jwt:Key"]).Returns("this-is-a-very-long-secret-key-that-meets-256-bit-requirements-for-hmac-sha256");
        mockConfigWithDefaults.Setup(x => x["Jwt:Issuer"]).Returns((string?)null);
        mockConfigWithDefaults.Setup(x => x["Jwt:Audience"]).Returns((string?)null);

        var tokenGeneratorWithDefaults = new JwtTokenGenerator(mockConfigWithDefaults.Object);
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com"
        };

        // Act
        var token = tokenGeneratorWithDefaults.GenerateToken(user);

        // Assert
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);

        jwtToken.Issuer.Should().Be("your-issuer");
        jwtToken.Audiences.Should().Contain("your-audience");
        token.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(999)]
    [InlineData(0)]
    [InlineData(-1)] // Edge case, though shouldn't happen in practice
    public void GenerateToken_VariousUserIds_ContainsCorrectUserId(int userId)
    {
        // Arrange
        var user = new User
        {
            Id = userId,
            Username = "testuser",
            Email = "test@example.com"
        };

        // Act
        var token = _jwtTokenGenerator.GenerateToken(user);

        // Assert
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);

        jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == userId.ToString());
    }

    [Theory]
    [InlineData("user1")]
    [InlineData("user_with_underscores")]
    [InlineData("user-with-dashes")]
    [InlineData("user123")]
    [InlineData("UPPERCASE_USER")]
    public void GenerateToken_VariousUsernames_ContainsCorrectUsername(string username)
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = username,
            Email = "test@example.com"
        };

        // Act
        var token = _jwtTokenGenerator.GenerateToken(user);

        // Assert
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);

        jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Name && c.Value == username);
    }

    [Fact]
    public void GenerateToken_ValidToken_CanBeValidatedWithSameConfiguration()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com"
        };
        var token = _jwtTokenGenerator.GenerateToken(user);

        // Act & Assert - Verify token can be validated
        var tokenHandler = new JwtSecurityTokenHandler();
        
        // This should not throw an exception if the token is valid
        Action validateAction = () => tokenHandler.ValidateToken(token, new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "test-issuer",
            ValidAudience = "test-audience",
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes("this-is-a-test-secret-key-that-is-long-enough-for-hmac-sha256"))
        }, out _);

        validateAction.Should().NotThrow();
    }
}
