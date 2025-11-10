using Microsoft.EntityFrameworkCore;
using MySvelteApp.Server.Shared.Domain.Entities;

namespace MySvelteApp.Server.Shared.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
}

