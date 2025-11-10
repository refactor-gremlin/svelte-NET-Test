using MySvelteApp.Server.Shared.Domain.ValueObjects;

namespace MySvelteApp.Server.Shared.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public Username Username { get; set; } = null!;
    public Email Email { get; set; } = null!;
    public string PasswordHash { get; set; } = string.Empty;
    public string PasswordSalt { get; set; } = string.Empty;
}

