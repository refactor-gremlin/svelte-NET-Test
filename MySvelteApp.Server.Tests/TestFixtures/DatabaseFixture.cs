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

    // Generic database management methods
    public async Task AddEntityAsync<T>(T entity) where T : class
    {
        DbContext.Set<T>().Add(entity);
        await DbContext.SaveChangesAsync();
    }

    public async Task AddEntitiesAsync<T>(params T[] entities) where T : class
    {
        DbContext.Set<T>().AddRange(entities);
        await DbContext.SaveChangesAsync();
    }

    public async Task<T?> GetEntityAsync<T>(int id) where T : class
    {
        return await DbContext.Set<T>().FindAsync(id);
    }

    public async Task<List<T>> GetAllEntitiesAsync<T>() where T : class
    {
        return await DbContext.Set<T>().ToListAsync();
    }

    public async Task<int> CountEntitiesAsync<T>() where T : class
    {
        return await DbContext.Set<T>().CountAsync();
    }

    public async Task ClearEntitiesAsync<T>() where T : class
    {
        DbContext.Set<T>().RemoveRange(DbContext.Set<T>());
        await DbContext.SaveChangesAsync();
    }

    public async Task ClearAllEntitiesAsync()
    {
        var entities = DbContext.Model.GetEntityTypes().Select(e => e.ClrType);
        foreach (var entityType in entities)
        {
            var method = typeof(DatabaseFixture)
                .GetMethod(nameof(ClearEntitiesAsync))
                ?.MakeGenericMethod(entityType);
            
            if (method != null)
            {
                await (Task)method.Invoke(this, null)!;
            }
        }
    }
}
