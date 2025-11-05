using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using MySvelteApp.Server.Infrastructure.Persistence;
using MySvelteApp.Server.Infrastructure.Persistence.Repositories;
using MySvelteApp.Server.Domain.Entities;
using MySvelteApp.Server.Tests.TestFixtures;

namespace MySvelteApp.Server.Tests.Infrastructure.Persistence;

public class UserRepositoryTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;

    public UserRepositoryTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetByUsernameAsync_ExistingUsername_ReturnsUser()
    {
        // Arrange
        await _fixture.CreateTestUserAsync("testuser", "test@example.com");
        var repository = new UserRepository(_fixture.DbContext);

        // Act
        var result = await repository.GetByUsernameAsync("testuser");

        // Assert
        result.Should().NotBeNull();
        result!.Username.Should().Be("testuser");
        result.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task GetByUsernameAsync_NonExistingUsername_ReturnsNull()
    {
        // Arrange
        var repository = new UserRepository(_fixture.DbContext);

        // Act
        var result = await repository.GetByUsernameAsync("nonexistentuser");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByUsernameAsync_CaseSensitive_ReturnsCorrectUser()
    {
        // Arrange
        await _fixture.CreateTestUserAsync("TestUser", "test@example.com");
        var repository = new UserRepository(_fixture.DbContext);

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
        var createdUser = await _fixture.CreateTestUserAsync("testuser", "test@example.com");
        var repository = new UserRepository(_fixture.DbContext);

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
        var repository = new UserRepository(_fixture.DbContext);

        // Act
        var result = await repository.GetByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UsernameExistsAsync_ExistingUsername_ReturnsTrue()
    {
        // Arrange
        await _fixture.CreateTestUserAsync("testuser", "test@example.com");
        var repository = new UserRepository(_fixture.DbContext);

        // Act
        var result = await repository.UsernameExistsAsync("testuser");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task UsernameExistsAsync_NonExistingUsername_ReturnsFalse()
    {
        // Arrange
        var repository = new UserRepository(_fixture.DbContext);

        // Act
        var result = await repository.UsernameExistsAsync("nonexistentuser");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task EmailExistsAsync_ExistingEmail_ReturnsTrue()
    {
        // Arrange
        await _fixture.CreateTestUserAsync("testuser", "test@example.com");
        var repository = new UserRepository(_fixture.DbContext);

        // Act
        var result = await repository.EmailExistsAsync("test@example.com");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task EmailExistsAsync_NonExistingEmail_ReturnsFalse()
    {
        // Arrange
        var repository = new UserRepository(_fixture.DbContext);

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
        var repository = new UserRepository(_fixture.DbContext);

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
        var repository = new UserRepository(_fixture.DbContext);

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
        var allUsers = await _fixture.DbContext.Users.ToListAsync();
        allUsers.Should().HaveCount(3);
    }

    [Fact]
    public async Task UsernameExistsAsync_CaseSensitive_ReturnsCorrectResult()
    {
        // Arrange
        await _fixture.CreateTestUserAsync("TestUser", "test@example.com");
        var repository = new UserRepository(_fixture.DbContext);

        // Act
        var result1 = await repository.UsernameExistsAsync("TestUser");
        var result2 = await repository.UsernameExistsAsync("testuser");

        // Assert
        result1.Should().BeTrue();
        result2.Should().BeFalse();
    }

    [Fact]
    public async Task EmailExistsAsync_CaseInsensitive_ReturnsTrueForDifferentCases()
    {
        // Arrange
        await _fixture.CreateTestUserAsync("testuser", "Test@Example.com");
        var repository = new UserRepository(_fixture.DbContext);

        // Act
        var result1 = await repository.EmailExistsAsync("Test@Example.com");
        var result2 = await repository.EmailExistsAsync("test@example.com");
        var result3 = await repository.EmailExistsAsync("TEST@EXAMPLE.COM");

        // Assert
        result1.Should().BeTrue();
        result2.Should().BeTrue();
        result3.Should().BeTrue();
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
        var repository = new UserRepository(_fixture.DbContext);

        // Act
        await repository.AddAsync(user);
        var savedUser = await repository.GetByIdAsync(user.Id);

        // Assert
        savedUser.Should().NotBeNull();
        savedUser!.Username.Should().Be(user.Username);
        savedUser.Email.Should().Be(user.Email);
    }

    [Fact]
    public async Task AddAsync_DuplicateUsername_ThrowsException()
    {
        // Arrange
        await _fixture.CreateTestUserAsync("testuser", "test@example.com");
        var repository = new UserRepository(_fixture.DbContext);
        var duplicateUser = new User
        {
            Username = "testuser",
            Email = "different@example.com",
            PasswordHash = "testhash",
            PasswordSalt = "testsalt"
        };

        // Act & Assert
        await repository.Invoking(async r => await r.AddAsync(duplicateUser))
            .Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task AddAsync_DuplicateEmail_ThrowsException()
    {
        // Arrange
        await _fixture.CreateTestUserAsync("testuser", "test@example.com");
        var repository = new UserRepository(_fixture.DbContext);
        var duplicateUser = new User
        {
            Username = "differentuser",
            Email = "test@example.com",
            PasswordHash = "testhash",
            PasswordSalt = "testsalt"
        };

        // Act & Assert
        await repository.Invoking(async r => await r.AddAsync(duplicateUser))
            .Should().ThrowAsync<DbUpdateException>();
    }
}
