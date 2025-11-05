using MySvelteApp.Server.Domain.Entities;
using MySvelteApp.Server.Application.Authentication.DTOs;

namespace MySvelteApp.Server.Tests.TestFixtures;

public static class UserTestDataFactory
{
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
}
