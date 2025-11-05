using MySvelteApp.Server.Domain.Entities;
using MySvelteApp.Server.Application.Authentication.DTOs;
using MySvelteApp.Server.Application.Pokemon.DTOs;

namespace MySvelteApp.Server.Tests.TestFixtures;

public static class GenericTestDataFactory
{
    // User-related factories
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

    // Generic entity factory method
    public static T CreateEntity<T>() where T : class, new()
    {
        return new T();
    }

    public static T CreateEntityWithProperties<T>(Action<T> configure) where T : class, new()
    {
        var entity = new T();
        configure(entity);
        return entity;
    }
}
