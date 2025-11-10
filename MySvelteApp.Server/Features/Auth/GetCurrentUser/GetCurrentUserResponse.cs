namespace MySvelteApp.Server.Features.Auth.GetCurrentUser;

public class GetCurrentUserResponse
{
    public UserDto User { get; set; } = new();
}

public class UserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

