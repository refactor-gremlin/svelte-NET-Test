using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using MySvelteApp.Server.Infrastructure.Persistence;
using MySvelteApp.Server.Application.Authentication.DTOs;
using MySvelteApp.Server.Presentation.Models.Auth;

namespace MySvelteApp.Server.Tests.Integration;

public class AuthenticationIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly IServiceScope _scope;
    private readonly AppDbContext _dbContext;

    public AuthenticationIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove the existing DbContext registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add in-memory database for testing
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDb");
                });
            });
        });

        _scope = _factory.Services.CreateScope();
        _dbContext = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _dbContext.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
        _scope.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task CompleteAuthenticationFlow_RegisterLoginGetCurrentUser_WorksCorrectly()
    {
        // Arrange
        var client = _factory.CreateClient();
        var registerRequest = new RegisterRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "Password123"
        };

        // Act 1: Register a new user
        var registerResponse = await client.PostAsJsonAsync("/Auth/register", registerRequest);
        registerResponse.EnsureSuccessStatusCode();
        
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<AuthSuccessResponse>();
        registerResult.Should().NotBeNull();
        registerResult!.Token.Should().NotBeNullOrEmpty();
        registerResult.Username.Should().Be("testuser");
        registerResult.UserId.Should().BeGreaterThan(0);

        // Act 2: Login with the registered user
        var loginRequest = new LoginRequest
        {
            Username = "testuser",
            Password = "Password123"
        };

        var loginResponse = await client.PostAsJsonAsync("/Auth/login", loginRequest);
        loginResponse.EnsureSuccessStatusCode();

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<AuthSuccessResponse>();
        loginResult.Should().NotBeNull();
        loginResult!.Token.Should().NotBeNullOrEmpty();
        loginResult.Username.Should().Be("testuser");

        // Act 3: Get current user with the token
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResult.Token);
        var meResponse = await client.GetAsync("/Auth/me");
        meResponse.EnsureSuccessStatusCode();

        var currentUser = await meResponse.Content.ReadFromJsonAsync<CurrentUserResponse>();
        currentUser.Should().NotBeNull();
        currentUser!.User.Should().NotBeNull();
        currentUser.User.Username.Should().Be("testuser");
        currentUser.User.Email.Should().Be("test@example.com");
        currentUser.User.Id.Should().Be(loginResult.UserId);
    }

    [Fact]
    public async Task Register_WithDuplicateUsername_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var registerRequest1 = new RegisterRequest
        {
            Username = "testuser",
            Email = "test1@example.com",
            Password = "Password123"
        };
        var registerRequest2 = new RegisterRequest
        {
            Username = "testuser", // Same username
            Email = "test2@example.com",
            Password = "Password456"
        };

        // Act 1: First registration should succeed
        var response1 = await client.PostAsJsonAsync("/Auth/register", registerRequest1);
        response1.EnsureSuccessStatusCode();

        // Act 2: Second registration with same username should fail
        var response2 = await client.PostAsJsonAsync("/Auth/register", registerRequest2);

        // Assert
        response2.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        var errorResponse = await response2.Content.ReadFromJsonAsync<AuthErrorResponse>();
        errorResponse.Should().NotBeNull();
        errorResponse!.Message.Should().Contain("username is already taken");
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var registerRequest1 = new RegisterRequest
        {
            Username = "user1",
            Email = "test@example.com",
            Password = "Password123"
        };
        var registerRequest2 = new RegisterRequest
        {
            Username = "user2",
            Email = "test@example.com", // Same email
            Password = "Password456"
        };

        // Act 1: First registration should succeed
        var response1 = await client.PostAsJsonAsync("/Auth/register", registerRequest1);
        response1.EnsureSuccessStatusCode();

        // Act 2: Second registration with same email should fail
        var response2 = await client.PostAsJsonAsync("/Auth/register", registerRequest2);

        // Assert
        response2.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        var errorResponse = await response2.Content.ReadFromJsonAsync<AuthErrorResponse>();
        errorResponse.Should().NotBeNull();
        errorResponse!.Message.Should().Contain("email is already registered");
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();
        var loginRequest = new LoginRequest
        {
            Username = "nonexistentuser",
            Password = "wrongpassword"
        };

        // Act
        var response = await client.PostAsJsonAsync("/Auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
        var errorResponse = await response.Content.ReadFromJsonAsync<AuthErrorResponse>();
        errorResponse.Should().NotBeNull();
        errorResponse!.Message.Should().Contain("Invalid username or password");
    }

    [Fact]
    public async Task GetCurrentUser_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/Auth/me");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCurrentUser_WithInvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");

        // Act
        var response = await client.GetAsync("/Auth/me");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("", "test@example.com", "Password123")]
    [InlineData("testuser", "", "Password123")]
    [InlineData("testuser", "test@example.com", "")]
    [InlineData("ab", "test@example.com", "Password123")]
    [InlineData("testuser", "invalid-email", "Password123")]
    [InlineData("testuser", "test@example.com", "weak")]
    [InlineData("testuser", "test@example.com", "password")]
    [InlineData("testuser", "test@example.com", "PASSWORD")]
    [InlineData("testuser", "test@example.com", "12345678")]
    public async Task Register_WithInvalidData_ReturnsBadRequest(string username, string email, string password)
    {
        // Arrange
        var client = _factory.CreateClient();
        var registerRequest = new RegisterRequest
        {
            Username = username,
            Email = email,
            Password = password
        };

        // Act
        var response = await client.PostAsJsonAsync("/Auth/register", registerRequest);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        var errorResponse = await response.Content.ReadFromJsonAsync<AuthErrorResponse>();
        errorResponse.Should().NotBeNull();
        errorResponse!.Message.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData("", "Password123")]
    [InlineData("testuser", "")]
    [InlineData("  ", "Password123")]
    [InlineData("testuser", "  ")]
    public async Task Login_WithInvalidData_ReturnsBadRequest(string username, string password)
    {
        // Arrange
        var client = _factory.CreateClient();
        var loginRequest = new LoginRequest
        {
            Username = username,
            Password = password
        };

        // Act
        var response = await client.PostAsJsonAsync("/Auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        var errorResponse = await response.Content.ReadFromJsonAsync<AuthErrorResponse>();
        errorResponse.Should().NotBeNull();
        errorResponse!.Message.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task MultipleUsers_RegisterAndLogin_WorksCorrectly()
    {
        // Arrange
        var client = _factory.CreateClient();
        var users = new List<RegisterRequest>
        {
            new() { Username = "user1", Email = "user1@example.com", Password = "Password123" },
            new() { Username = "user2", Email = "user2@example.com", Password = "Password456" },
            new() { Username = "user3", Email = "user3@example.com", Password = "Password789" }
        };

        var tokens = new List<string>();

        // Act & Assert - Register all users
        foreach (var user in users)
        {
            var registerResponse = await client.PostAsJsonAsync("/Auth/register", user);
            registerResponse.EnsureSuccessStatusCode();
            
            var registerResult = await registerResponse.Content.ReadFromJsonAsync<AuthSuccessResponse>();
            registerResult.Should().NotBeNull();
            registerResult!.Token.Should().NotBeNullOrEmpty();
            tokens.Add(registerResult.Token);
        }

        // Act & Assert - Login with each user
        for (int i = 0; i < users.Count; i++)
        {
            var loginRequest = new LoginRequest
            {
                Username = users[i].Username,
                Password = users[i].Password
            };

            var loginResponse = await client.PostAsJsonAsync("/Auth/login", loginRequest);
            loginResponse.EnsureSuccessStatusCode();

            var loginResult = await loginResponse.Content.ReadFromJsonAsync<AuthSuccessResponse>();
            loginResult.Should().NotBeNull();
            loginResult!.Token.Should().NotBeNullOrEmpty();
            loginResult.Username.Should().Be(users[i].Username);
        }
    }

    [Fact]
    public async Task TokenValidation_MultipleRequests_WithSameToken_WorksCorrectly()
    {
        // Arrange
        var client = _factory.CreateClient();
        var registerRequest = new RegisterRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "Password123"
        };

        // Act 1: Register and get token
        var registerResponse = await client.PostAsJsonAsync("/Auth/register", registerRequest);
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<AuthSuccessResponse>();
        var token = registerResult!.Token;

        // Act 2: Use the same token for multiple requests
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        for (int i = 0; i < 5; i++)
        {
            var meResponse = await client.GetAsync("/Auth/me");
            meResponse.EnsureSuccessStatusCode();

            var currentUser = await meResponse.Content.ReadFromJsonAsync<CurrentUserResponse>();
            currentUser.Should().NotBeNull();
            currentUser!.User.Username.Should().Be("testuser");
        }

        // Assert - All requests should succeed with the same token
    }
}
