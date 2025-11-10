using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using MySvelteApp.Server.Shared.Infrastructure.Persistence;
using MySvelteApp.Server.Shared.Infrastructure.Persistence.Repositories;
using MySvelteApp.Server.Shared.Domain.Entities;
using MySvelteApp.Server.Tests.TestFixtures;

namespace MySvelteApp.Server.Tests.Shared.Infrastructure.Persistence;

public class UserRepositoryTests : TestBase
{
    protected UserRepository Repository => new(DbContext);

    [Fact]
    public async Task GetByUsernameAsync_ExistingUsername_ReturnsUser()
    {
        // Arrange
        var user = GenericTestDataFactory.CreateUser(username: "testuser", email: "test@example.com");
        await AddEntityAsync(user);

        // Act
        var result = await Repository.GetByUsernameAsync("testuser");

        // Assert
        result.Should().NotBeNull();
        result!.Username.Should().Be("testuser");
        result.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task GetByUsernameAsync_NonExistingUsername_ReturnsNull()
    {
        // Arrange
        // No users in database

        // Act
        var result = await Repository.GetByUsernameAsync("nonexistentuser");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByUsernameAsync_CaseSensitive_ReturnsCorrectUser()
    {
        // Arrange
        var user = GenericTestDataFactory.CreateUser(username: "TestUser", email: "test@example.com");
        await AddEntityAsync(user);

        // Act
        var result1 = await Repository.GetByUsernameAsync("TestUser");
        var result2 = await Repository.GetByUsernameAsync("testuser");

        // Assert
        result1.Should().NotBeNull();
        result1!.Username.Should().Be("TestUser");
        result2.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ExistingUserId_ReturnsUser()
    {
        // Arrange
        var user = GenericTestDataFactory.CreateUser(id: 1, username: "testuser", email: "test@example.com");
        await AddEntityAsync(user);

        // Act
        var result = await Repository.GetByIdAsync(user.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
        result.Username.Should().Be("testuser");
        result.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingUserId_ReturnsNull()
    {
        // Arrange
        // No users in database

        // Act
        var result = await Repository.GetByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UsernameExistsAsync_ExistingUsername_ReturnsTrue()
    {
        // Arrange
        var user = GenericTestDataFactory.CreateUser(username: "testuser", email: "test@example.com");
        await AddEntityAsync(user);

        // Act
        var result = await Repository.UsernameExistsAsync("testuser");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task UsernameExistsAsync_NonExistingUsername_ReturnsFalse()
    {
        // Arrange
        // No users in database

        // Act
        var result = await Repository.UsernameExistsAsync("nonexistentuser");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task EmailExistsAsync_ExistingEmail_ReturnsTrue()
    {
        // Arrange
        var user = GenericTestDataFactory.CreateUser(username: "testuser", email: "test@example.com");
        await AddEntityAsync(user);

        // Act
        var result = await Repository.EmailExistsAsync("test@example.com");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task EmailExistsAsync_NonExistingEmail_ReturnsFalse()
    {
        // Arrange
        // No users in database

        // Act
        var result = await Repository.EmailExistsAsync("nonexistent@example.com");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task AddAsync_ValidUser_ReturnsSuccess()
    {
        // Arrange
        var user = GenericTestDataFactory.CreateUser(
            username: "newuser",
            email: "newuser@example.com",
            passwordHash: "testhash",
            passwordSalt: "testsalt");

        // Act
        await Repository.AddAsync(user);
        var savedUser = await Repository.GetByIdAsync(user.Id);

        // Assert
        savedUser.Should().NotBeNull();
        savedUser!.Username.Should().Be(user.Username);
        savedUser.Email.Should().Be(user.Email);
        savedUser.PasswordHash.Should().Be(user.PasswordHash);
        savedUser.PasswordSalt.Should().Be(user.PasswordSalt);
    }

    [Fact]
    public async Task AddAsync_MultipleUsers_AllPersistedCorrectly()
    {
        // Arrange
        var users = new List<User>
        {
            GenericTestDataFactory.CreateUser(id: 1, username: "user1", email: "user1@example.com", passwordHash: "hash1", passwordSalt: "salt1"),
            GenericTestDataFactory.CreateUser(id: 2, username: "user2", email: "user2@example.com", passwordHash: "hash2", passwordSalt: "salt2"),
            GenericTestDataFactory.CreateUser(id: 3, username: "user3", email: "user3@example.com", passwordHash: "hash3", passwordSalt: "salt3")
        };

        // Act
        foreach (var user in users)
        {
            await Repository.AddAsync(user);
        }

        // Assert
        foreach (var user in users)
        {
            var savedUser = await Repository.GetByIdAsync(user.Id);
            savedUser.Should().NotBeNull();
            savedUser!.Username.Should().Be(user.Username);
            savedUser.Email.Should().Be(user.Email);
        }

        // Verify total count
        var allUsers = await DbContext.Users.ToListAsync();
        allUsers.Should().HaveCount(3);
    }

    [Fact]
    public async Task UsernameExistsAsync_CaseSensitive_ReturnsCorrectResult()
    {
        // Arrange
        var user = GenericTestDataFactory.CreateUser(username: "TestUser", email: "test@example.com");
        await AddEntityAsync(user);

        // Act
        var result1 = await Repository.UsernameExistsAsync("TestUser");
        var result2 = await Repository.UsernameExistsAsync("testuser");

        // Assert
        result1.Should().BeTrue();
        result2.Should().BeFalse();
    }

    [Fact]
    public async Task EmailExistsAsync_CaseSensitive_ReturnsFalseForDifferentCases()
    {
        // Arrange
        var user = GenericTestDataFactory.CreateUser(username: "testuser", email: "Test@Example.com");
        await AddEntityAsync(user);

        // Act
        var result1 = await Repository.EmailExistsAsync("Test@Example.com");
        var result2 = await Repository.EmailExistsAsync("test@example.com");
        var result3 = await Repository.EmailExistsAsync("TEST@EXAMPLE.COM");

        // Assert - EF Core InMemory is case-sensitive for strings by default
        result1.Should().BeTrue();  // Exact match
        result2.Should().BeFalse(); // Different case
        result3.Should().BeFalse(); // Different case
    }

    [Fact]
    public async Task AddAsync_UserWithSpecialCharacters_PersistedCorrectly()
    {
        // Arrange
        var user = GenericTestDataFactory.CreateUser(
            username: "user_with-dashes123",
            email: "user+test@example.co.uk",
            passwordHash: "testhash",
            passwordSalt: "testsalt");

        // Act
        await Repository.AddAsync(user);
        var savedUser = await Repository.GetByIdAsync(user.Id);

        // Assert
        savedUser.Should().NotBeNull();
        savedUser!.Username.Should().Be(user.Username);
        savedUser.Email.Should().Be(user.Email);
    }

    [Fact]
    public async Task AddAsync_DuplicateUsername_AllowsInMemoryDatabase()
    {
        // Arrange
        var existingUser = GenericTestDataFactory.CreateUser(username: "testuser", email: "test@example.com");
        await AddEntityAsync(existingUser);
        
        var duplicateUser = GenericTestDataFactory.CreateUser(
            username: "testuser",
            email: "different@example.com",
            passwordHash: "testhash",
            passwordSalt: "testsalt");

        // Act & Assert - In-memory database doesn't enforce uniqueness constraints
        // This test documents the behavior - in production SQL would throw an exception
        await Repository.Invoking(async r => await r.AddAsync(duplicateUser))
            .Should().NotThrowAsync();
    }

    [Fact]
    public async Task AddAsync_DuplicateEmail_AllowsInMemoryDatabase()
    {
        // Arrange
        var existingUser = GenericTestDataFactory.CreateUser(username: "testuser", email: "test@example.com");
        await AddEntityAsync(existingUser);
        
        var duplicateUser = GenericTestDataFactory.CreateUser(
            username: "differentuser",
            email: "test@example.com",
            passwordHash: "testhash",
            passwordSalt: "testsalt");

        // Act & Assert - In-memory database doesn't enforce uniqueness constraints
        // This test documents the behavior - in production SQL would throw an exception
        await Repository.Invoking(async r => await r.AddAsync(duplicateUser))
            .Should().NotThrowAsync();
    }
}
