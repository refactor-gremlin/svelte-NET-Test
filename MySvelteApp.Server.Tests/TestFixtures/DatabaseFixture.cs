using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MySvelteApp.Server.Infrastructure.Persistence;
using MySvelteApp.Server.Domain.Entities;

namespace MySvelteApp.Server.Tests.TestFixtures;

public class DatabaseFixture : IDisposable
{
    public AppDbContext DbContext { get; private set; }

    public DatabaseFixture()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        DbContext = new AppDbContext(options);
        DbContext.Database.EnsureCreated();
    }

    public void Dispose()
    {
        DbContext.Database.EnsureDeleted();
        DbContext.Dispose();
    }

    public async Task<User> CreateTestUserAsync(string username = "testuser", string email = "test@example.com")
    {
        var user = new User
        {
            Username = username,
            Email = email,
            PasswordHash = "testhash",
            PasswordSalt = "testsalt"
        };

        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();

        return user;
    }

    public async Task<List<User>> CreateMultipleTestUsersAsync(int count)
    {
        var users = new List<User>();
        for (int i = 1; i <= count; i++)
        {
            users.Add(await CreateTestUserAsync($"user{i}", $"user{i}@example.com"));
        }
        return users;
    }
}
