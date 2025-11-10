using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Json;
using FluentAssertions;
using MySvelteApp.Server.Features.Auth.RegisterUser;
using MySvelteApp.Server.Features.Auth.LoginUser;
using MySvelteApp.Server.Features.Auth.GetCurrentUser;
using MySvelteApp.Server.Shared.Common.DTOs.Responses;
using MySvelteApp.Server.Tests.TestFixtures;

namespace MySvelteApp.Server.Tests.Integration;

public class AuthenticationIntegrationTests : IntegrationTestTemplate
{
    public AuthenticationIntegrationTests(WebApplicationFactory<Program> factory) 
        : base(factory) { }

    [Fact]
    public async Task CompleteAuthenticationFlow_RegisterLoginGetCurrentUser_WorksCorrectly()
    {
        // Arrange
        var registerRequest = GenericTestDataFactory.CreateRegisterRequest();

        // Act 1: Register a new user
        var registerResponse = await Client.PostAsJsonAsync("/auth/register", registerRequest);
        AssertSuccessResponse(registerResponse);
        
        var registerResult = await HttpClientTestHelpers.ReadJsonAsync<ApiResponse<RegisterUserResponse>>(registerResponse);
        registerResult.Should().NotBeNull();
        registerResult!.Success.Should().BeTrue();
        registerResult.Data.Token.Should().NotBeNullOrEmpty();
        registerResult.Data.User.Should().NotBeNull();
        registerResult.Data.User.Username.Should().Be("testuser");
        registerResult.Data.User.Id.Should().BeGreaterThan(0);

        // Act 2: Login with the registered user
        var loginRequest = GenericTestDataFactory.CreateLoginRequest();

        var loginResponse = await Client.PostAsJsonAsync("/auth/login", loginRequest);
        AssertSuccessResponse(loginResponse);

        var loginResult = await HttpClientTestHelpers.ReadJsonAsync<ApiResponse<LoginUserResponse>>(loginResponse);
        loginResult.Should().NotBeNull();
        loginResult!.Success.Should().BeTrue();
        loginResult.Data.Token.Should().NotBeNullOrEmpty();
        loginResult.Data.User.Should().NotBeNull();
        loginResult.Data.User.Username.Should().Be("testuser");

        // Act 3: Get current user with the token
        SetBearerToken(loginResult.Data.Token);
        var meResponse = await Client.GetAsync("/auth/me");
        AssertSuccessResponse(meResponse);

        var currentUser = await HttpClientTestHelpers.ReadJsonAsync<ApiResponse<GetCurrentUserResponse>>(meResponse);
        currentUser.Should().NotBeNull();
        currentUser!.Success.Should().BeTrue();
        currentUser.Data.User.Should().NotBeNull();
        currentUser.Data.User.Username.Should().Be("testuser");
        currentUser.Data.User.Email.Should().Be("test@example.com");
        currentUser.Data.User.Id.Should().Be(loginResult.Data.User.Id);
    }

    [Fact]
    public async Task Register_WithDuplicateUsername_ReturnsBadRequest()
    {
        // Arrange
        var registerRequest1 = GenericTestDataFactory.CreateRegisterRequest(
            username: "testuser",
            email: "test1@example.com");
        var registerRequest2 = GenericTestDataFactory.CreateRegisterRequest(
            username: "testuser", // Same username
            email: "test2@example.com");

        // Act 1: First registration should succeed
        var response1 = await Client.PostAsJsonAsync("/auth/register", registerRequest1);
        AssertSuccessResponse(response1);

        // Act 2: Second registration with same username should fail
        var response2 = await Client.PostAsJsonAsync("/auth/register", registerRequest2);

        // Assert
        AssertErrorResponse(response2, System.Net.HttpStatusCode.Conflict, "username is already taken");
        var errorResponse = await HttpClientTestHelpers.ReadJsonAsync<ApiErrorResponse>(response2);
        errorResponse.Should().NotBeNull();
        errorResponse!.Message.Should().Contain("username is already taken");
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsBadRequest()
    {
        // Arrange
        var registerRequest1 = GenericTestDataFactory.CreateRegisterRequest(
            username: "user1",
            email: "test@example.com");
        var registerRequest2 = GenericTestDataFactory.CreateRegisterRequest(
            username: "user2",
            email: "test@example.com"); // Same email

        // Act 1: First registration should succeed
        var response1 = await Client.PostAsJsonAsync("/auth/register", registerRequest1);
        AssertSuccessResponse(response1);

        // Act 2: Second registration with same email should fail
        var response2 = await Client.PostAsJsonAsync("/auth/register", registerRequest2);

        // Assert
        AssertErrorResponse(response2, System.Net.HttpStatusCode.Conflict, "email is already registered");
        var errorResponse = await HttpClientTestHelpers.ReadJsonAsync<ApiErrorResponse>(response2);
        errorResponse.Should().NotBeNull();
        errorResponse!.Message.Should().Contain("email is already registered");
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var loginRequest = GenericTestDataFactory.CreateLoginRequest(
            username: "nonexistentuser",
            password: "wrongpassword");

        // Act
        var response = await Client.PostAsJsonAsync("/auth/login", loginRequest);

        // Assert
        AssertErrorResponse(response, System.Net.HttpStatusCode.Unauthorized, "Invalid username or password");
        var errorResponse = await HttpClientTestHelpers.ReadJsonAsync<ApiErrorResponse>(response);
        errorResponse.Should().NotBeNull();
        errorResponse!.Message.Should().Contain("Invalid username or password");
    }

    [Fact]
    public async Task GetCurrentUser_WithoutToken_ReturnsUnauthorized()
    {
        // Arrange - Client has no token

        // Act
        var response = await Client.GetAsync("/auth/me");

        // Assert
        AssertErrorResponse(response, System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCurrentUser_WithInvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        SetBearerToken("invalid-token");

        // Act
        var response = await Client.GetAsync("/auth/me");

        // Assert
        AssertErrorResponse(response, System.Net.HttpStatusCode.Unauthorized);
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
        var registerRequest = GenericTestDataFactory.CreateRegisterRequest(
            username: username,
            email: email,
            password: password);

        // Act
        var response = await Client.PostAsJsonAsync("/auth/register", registerRequest);

        // Assert
        AssertErrorResponse(response, System.Net.HttpStatusCode.BadRequest);
        var errorResponse = await HttpClientTestHelpers.ReadJsonAsync<ApiErrorResponse>(response);
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
        var loginRequest = GenericTestDataFactory.CreateLoginRequest(
            username: username,
            password: password);

        // Act
        var response = await Client.PostAsJsonAsync("/auth/login", loginRequest);

        // Assert
        AssertErrorResponse(response, System.Net.HttpStatusCode.BadRequest);
        var errorResponse = await HttpClientTestHelpers.ReadJsonAsync<ApiErrorResponse>(response);
        errorResponse.Should().NotBeNull();
        errorResponse!.Message.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task MultipleUsers_RegisterAndLogin_WorksCorrectly()
    {
        // Arrange
        var users = new List<RegisterUserRequest>
        {
            GenericTestDataFactory.CreateRegisterRequest("user1", "user1@example.com", "Password123"),
            GenericTestDataFactory.CreateRegisterRequest("user2", "user2@example.com", "Password456"),
            GenericTestDataFactory.CreateRegisterRequest("user3", "user3@example.com", "Password789")
        };

        var tokens = new List<string>();

        // Act & Assert - Register all users
        foreach (var user in users)
        {
            var registerResponse = await Client.PostAsJsonAsync("/auth/register", user);
            AssertSuccessResponse(registerResponse);
            
            var registerResult = await HttpClientTestHelpers.ReadJsonAsync<ApiResponse<RegisterUserResponse>>(registerResponse);
            registerResult.Should().NotBeNull();
            registerResult!.Success.Should().BeTrue();
            registerResult.Data.Token.Should().NotBeNullOrEmpty();
            tokens.Add(registerResult.Data.Token);
        }

        // Act & Assert - Login with each user
        for (int i = 0; i < users.Count; i++)
        {
            var loginRequest = GenericTestDataFactory.CreateLoginRequest(
                username: users[i].Username,
                password: users[i].Password);

            var loginResponse = await Client.PostAsJsonAsync("/auth/login", loginRequest);
            AssertSuccessResponse(loginResponse);

            var loginResult = await HttpClientTestHelpers.ReadJsonAsync<ApiResponse<LoginUserResponse>>(loginResponse);
            loginResult.Should().NotBeNull();
            loginResult!.Success.Should().BeTrue();
            loginResult.Data.Token.Should().NotBeNullOrEmpty();
            loginResult.Data.User.Should().NotBeNull();
            loginResult.Data.User.Username.Should().Be(users[i].Username);
        }
    }

    [Fact]
    public async Task TokenValidation_MultipleRequests_WithSameToken_WorksCorrectly()
    {
        // Arrange
        var registerRequest = GenericTestDataFactory.CreateRegisterRequest();

        // Act 1: Register and get token
        var registerResponse = await Client.PostAsJsonAsync("/auth/register", registerRequest);
        AssertSuccessResponse(registerResponse);
        var registerResult = await HttpClientTestHelpers.ReadJsonAsync<ApiResponse<RegisterUserResponse>>(registerResponse);
        var token = registerResult!.Data.Token;

        // Act 2: Use the same token for multiple requests
        SetBearerToken(token);

        for (int i = 0; i < 5; i++)
        {
            var meResponse = await Client.GetAsync("/auth/me");
            AssertSuccessResponse(meResponse);

            var currentUser = await HttpClientTestHelpers.ReadJsonAsync<ApiResponse<GetCurrentUserResponse>>(meResponse);
            currentUser.Should().NotBeNull();
            currentUser!.Success.Should().BeTrue();
            currentUser.Data.User.Username.Should().Be("testuser");
        }

        // Assert - All requests should succeed with the same token
    }
}
