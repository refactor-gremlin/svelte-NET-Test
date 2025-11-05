using Microsoft.EntityFrameworkCore;
using MySvelteApp.Server.Infrastructure.Persistence;
using MySvelteApp.Server.Domain.Entities;
using Xunit;

namespace MySvelteApp.Server.Tests.TestFixtures;

public abstract class TestBase : IAsyncLifetime
{
    protected AppDbContext DbContext { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        DbContext = new AppDbContext(options);
        await DbContext.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await DbContext.Database.EnsureDeletedAsync();
        await DbContext.DisposeAsync();
    }

    // Generic methods for any entity
    protected async Task<T> AddEntityAsync<T>(T entity) where T : class
    {
        DbContext.Set<T>().Add(entity);
        await DbContext.SaveChangesAsync();
        return entity;
    }

    protected async Task<List<T>> AddEntitiesAsync<T>(params T[] entities) where T : class
    {
        DbContext.Set<T>().AddRange(entities);
        await DbContext.SaveChangesAsync();
        return entities.ToList();
    }

    protected async Task<T?> GetEntityAsync<T>(int id) where T : class
    {
        return await DbContext.Set<T>().FindAsync(id);
    }

    protected async Task<List<T>> GetAllEntitiesAsync<T>() where T : class
    {
        return await DbContext.Set<T>().ToListAsync();
    }

    protected async Task<int> CountEntitiesAsync<T>() where T : class
    {
        return await DbContext.Set<T>().CountAsync();
    }

    protected async Task ClearEntitiesAsync<T>() where T : class
    {
        DbContext.Set<T>().RemoveRange(DbContext.Set<T>());
        await DbContext.SaveChangesAsync();
    }

    // Specific User methods (keeping for backward compatibility)
    protected async Task<User> CreateTestUserAsync(string username = "testuser", string email = "test@example.com")
    {
        var user = new User
        {
            Username = username,
            Email = email,
            PasswordHash = "testhash",
            PasswordSalt = "testsalt"
        };

        return await AddEntityAsync(user);
    }
}
