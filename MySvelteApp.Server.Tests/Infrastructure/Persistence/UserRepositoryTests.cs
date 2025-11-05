using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using MySvelteApp.Server.Infrastructure.Persistence;
using MySvelteApp.Server.Infrastructure.Persistence.Repositories;
using MySvelteApp.Server.Domain.Entities;
using MySvelteApp.Server.Tests.TestFixtures;

namespace MySvelteApp.Server.Tests.Infrastructure.Persistence;

public class UserRepositoryTests : TestBase
{
    protected UserRepository Repository => new(DbContext);

    [Fact]
    public async Task GetByUsernameAsync_ExistingUsername_ReturnsUser()
    {
        // Arrange
        await CreateTestUserAsync("testuser", "test@example.com");

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
        var repository = Repository;

        // Act
        var result = await repository.GetByUsernameAsync("nonexistentuser");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByUsernameAsync_CaseSensitive_ReturnsCorrectUser()
    {
        // Arrange
        
        await CreateTestUserAsync("TestUser", "test@example.com");
        var repository = Repository;

        // Act
        var result1 = await repository.GetByUsernameAsync("TestUser");
        var result2 = await repository.GetByUsernameAsync("testuser");

        // Assert
        result1.Should().NotBeNull();
        result1!.Username.Should().Be("TestUser");
        result2.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ExistingUserId_ReturnsUser()
    {
        // Arrange
        var createdUser = await CreateTestUserAsync("testuser", "test@example.com");
        var repository = Repository;

        // Act
        var result = await repository.GetByIdAsync(createdUser.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(createdUser.Id);
        result.Username.Should().Be("testuser");
        result.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingUserId_ReturnsNull()
    {
        // Arrange
        var repository = Repository;

        // Act
        var result = await repository.GetByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UsernameExistsAsync_ExistingUsername_ReturnsTrue()
    {
        // Arrange
        await CreateTestUserAsync("testuser", "test@example.com");
        var repository = Repository;

        // Act
        var result = await repository.UsernameExistsAsync("testuser");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task UsernameExistsAsync_NonExistingUsername_ReturnsFalse()
    {
        // Arrange
        var repository = Repository;

        // Act
        var result = await repository.UsernameExistsAsync("nonexistentuser");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task EmailExistsAsync_ExistingEmail_ReturnsTrue()
    {
        // Arrange
        await CreateTestUserAsync("testuser", "test@example.com");
        var repository = Repository;

        // Act
        var result = await repository.EmailExistsAsync("test@example.com");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task EmailExistsAsync_NonExistingEmail_ReturnsFalse()
    {
        // Arrange
        var repository = Repository;

        // Act
        var result = await repository.EmailExistsAsync("nonexistent@example.com");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task AddAsync_ValidUser_ReturnsSuccess()
    {
        // Arrange
        var user = new User
        {
            Username = "newuser",
            Email = "newuser@example.com",
            PasswordHash = "testhash",
            PasswordSalt = "testsalt"
        };
        var repository = Repository;

        // Act
        await repository.AddAsync(user);
        var savedUser = await repository.GetByIdAsync(user.Id);

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
            new User { Username = "user1", Email = "user1@example.com", PasswordHash = "hash1", PasswordSalt = "salt1" },
            new User { Username = "user2", Email = "user2@example.com", PasswordHash = "hash2", PasswordSalt = "salt2" },
            new User { Username = "user3", Email = "user3@example.com", PasswordHash = "hash3", PasswordSalt = "salt3" }
        };
        var repository = Repository;

        // Act
        foreach (var user in users)
        {
            await repository.AddAsync(user);
        }

        // Assert
        foreach (var user in users)
        {
            var savedUser = await repository.GetByIdAsync(user.Id);
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
        
        await CreateTestUserAsync("TestUser", "test@example.com");
        var repository = Repository;

        // Act
        var result1 = await repository.UsernameExistsAsync("TestUser");
        var result2 = await repository.UsernameExistsAsync("testuser");

        // Assert
        result1.Should().BeTrue();
        result2.Should().BeFalse();
    }

    [Fact]
    public async Task EmailExistsAsync_CaseSensitive_ReturnsFalseForDifferentCases()
    {
        // Arrange
        
        await CreateTestUserAsync("testuser", "Test@Example.com");
        var repository = Repository;

        // Act
        var result1 = await repository.EmailExistsAsync("Test@Example.com");
        var result2 = await repository.EmailExistsAsync("test@example.com");
        var result3 = await repository.EmailExistsAsync("TEST@EXAMPLE.COM");

        // Assert - EF Core InMemory is case-sensitive for strings by default
        result1.Should().BeTrue();  // Exact match
        result2.Should().BeFalse(); // Different case
        result3.Should().BeFalse(); // Different case
    }

    [Fact]
    public async Task AddAsync_UserWithSpecialCharacters_PersistedCorrectly()
    {
        // Arrange
        var user = new User
        {
            Username = "user_with-dashes123",
            Email = "user+test@example.co.uk",
            PasswordHash = "testhash",
            PasswordSalt = "testsalt"
        };
        var repository = Repository;

        // Act
        await repository.AddAsync(user);
        var savedUser = await repository.GetByIdAsync(user.Id);

        // Assert
        savedUser.Should().NotBeNull();
        savedUser!.Username.Should().Be(user.Username);
        savedUser.Email.Should().Be(user.Email);
    }

    [Fact]
    public async Task AddAsync_DuplicateUsername_AllowsInMemoryDatabase()
    {
        // Arrange
        await CreateTestUserAsync("testuser", "test@example.com");
        var repository = Repository;
        var duplicateUser = new User
        {
            Username = "testuser",
            Email = "different@example.com",
            PasswordHash = "testhash",
            PasswordSalt = "testsalt"
        };

        // Act & Assert - In-memory database doesn't enforce uniqueness constraints
        // This test documents the behavior - in production SQL would throw an exception
        await repository.Invoking(async r => await r.AddAsync(duplicateUser))
            .Should().NotThrowAsync();
    }

    [Fact]
    public async Task AddAsync_DuplicateEmail_AllowsInMemoryDatabase()
    {
        // Arrange
        await CreateTestUserAsync("testuser", "test@example.com");
        var repository = Repository;
        var duplicateUser = new User
        {
            Username = "differentuser",
            Email = "test@example.com",
            PasswordHash = "testhash",
            PasswordSalt = "testsalt"
        };

        // Act & Assert - In-memory database doesn't enforce uniqueness constraints
        // This test documents the behavior - in production SQL would throw an exception
        await repository.Invoking(async r => await r.AddAsync(duplicateUser))
            .Should().NotThrowAsync();
    }
}
