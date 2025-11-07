using MySvelteApp.Server.Domain.Entities;
using MySvelteApp.Server.Application.Authentication.DTOs;
using MySvelteApp.Server.Application.Pokemon.DTOs;

namespace MySvelteApp.Server.Tests.TestFixtures;

/// <summary>
/// Generic factory for creating test data for any entity type.
/// Use this to quickly create test data for new features.
/// 
/// Examples:
/// // For new entities, use the generic methods:
/// var entity = GenericTestDataFactory.CreateEntityWithProperties<YourEntity>(e => 
/// {
///     e.Name = "test";
///     e.Value = 42;
/// });
/// 
/// // For multiple entities:
/// var entities = GenericTestDataFactory.CreateMultipleEntities<YourEntity>(3, (e, index) =>
/// {
///     e.Name = $"entity{index}";
///     e.Id = index;
/// });
/// 
/// // For DTOs, add specific factory methods like CreateRegisterRequest below
/// </summary>
public static class GenericTestDataFactory
{
    // Generic entity factory methods - use these for new features
    
    /// <summary>
    /// Creates a new entity instance with default constructor.
    /// </summary>
    public static T CreateEntity<T>() where T : class, new()
    {
        return new T();
    }

    /// <summary>
    /// Creates an entity with configured properties.
    /// Use this pattern for new features:
    /// var user = GenericTestDataFactory.CreateEntityWithProperties<User>(u => 
    /// {
    ///     u.Username = "testuser";
    ///     u.Email = "test@example.com";
    /// });
    /// </summary>
    public static T CreateEntityWithProperties<T>(Action<T> configure) where T : class, new()
    {
        var entity = new T();
        configure(entity);
        return entity;
    }

    /// <summary>
    /// Creates multiple entities with incremental properties.
    /// Use for testing list operations:
    /// var users = GenericTestDataFactory.CreateMultipleEntities<User>(3, (user, index) =>
    /// {
    ///     user.Username = $"user{index}";
    ///     user.Email = $"user{index}@example.com";
    /// });
    /// </summary>
    public static List<T> CreateMultipleEntities<T>(int count, Action<T, int>? configure = null) where T : class, new()
    {
        var entities = new List<T>();
        for (int i = 0; i < count; i++)
        {
            var entity = new T();
            configure?.Invoke(entity, i + 1);
            entities.Add(entity);
        }
        return entities;
    }

    /// <summary>
    /// Creates test data with a specific seed for reproducible tests.
    /// Use when you need consistent test data:
    /// var user = GenericTestDataFactory.CreateSeededEntity<User>("test-seed", user => 
    /// {
    ///     user.Username = "consistent-test-user";
    /// });
    /// </summary>
    public static T CreateSeededEntity<T>(string seed, Action<T> configure) where T : class, new()
    {
        var entity = new T();
        configure(entity);
        return entity;
    }

    // Specific factory methods for existing DTOs and entities
    // Add new factory methods here when adding new features

    // Auth DTO factories
    public static RegisterRequest CreateRegisterRequest(
        string username = "testuser",
        string email = "test@example.com",
        string password = "Password123")
    {
        return new RegisterRequest
        {
            Username = username,
            Email = email,
            Password = password
        };
    }

    public static LoginRequest CreateLoginRequest(
        string username = "testuser",
        string password = "Password123")
    {
        return new LoginRequest
        {
            Username = username,
            Password = password
        };
    }

    public static List<RegisterRequest> CreateMultipleRegisterRequests(int count)
    {
        var requests = new List<RegisterRequest>();
        for (int i = 1; i <= count; i++)
        {
            requests.Add(CreateRegisterRequest($"user{i}", $"user{i}@example.com", $"Password{i}"));
        }
        return requests;
    }

    // User entity factory
    public static User CreateUser(
        int id = 1,
        string username = "testuser",
        string email = "test@example.com",
        string passwordHash = "testhash",
        string passwordSalt = "testsalt")
    {
        return new User
        {
            Id = id,
            Username = username,
            Email = email,
            PasswordHash = passwordHash,
            PasswordSalt = passwordSalt
        };
    }

    public static List<User> CreateMultipleUsers(int count)
    {
        var users = new List<User>();
        for (int i = 1; i <= count; i++)
        {
            users.Add(CreateUser(
                id: i,
                username: $"user{i}",
                email: $"user{i}@example.com",
                passwordHash: $"hash{i}",
                passwordSalt: $"salt{i}"
            ));
        }
        return users;
    }

    // Pokemon DTO factories
    public static RandomPokemonDto CreateRandomPokemonDto(
        string name = "pikachu",
        string type = "electric",
        string image = "https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/pokemon/25.png")
    {
        return new RandomPokemonDto
        {
            Name = name,
            Type = type,
            Image = image
        };
    }

    public static List<RandomPokemonDto> CreateMultipleRandomPokemonDtos(int count)
    {
        var pokemons = new List<RandomPokemonDto>();
        for (int i = 1; i <= count; i++)
        {
            pokemons.Add(CreateRandomPokemonDto($"pokemon{i}", $"type{i}", $"https://example.com/pokemon{i}.png"));
        }
        return pokemons;
    }
}
