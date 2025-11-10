namespace MySvelteApp.Server.Shared.Common.DTOs;

/// <summary>
/// Standard user data transfer object used across user-related endpoints.
/// Use this when returning user information to ensure consistency across the API.
/// </summary>
public class UserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

