using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MySvelteApp.Server.Shared.Domain.Entities;
using MySvelteApp.Server.Shared.Domain.ValueObjects;

namespace MySvelteApp.Server.Shared.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure value object conversions
        var emailConverter = new ValueConverter<Email, string>(
            v => v.Value,
            v => Email.Create(v));

        var usernameConverter = new ValueConverter<Username, string>(
            v => v.Value,
            v => Username.Create(v));

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.Email)
                .HasConversion(emailConverter)
                .HasMaxLength(255);

            entity.Property(e => e.Username)
                .HasConversion(usernameConverter)
                .HasMaxLength(50);
        });
    }
}

