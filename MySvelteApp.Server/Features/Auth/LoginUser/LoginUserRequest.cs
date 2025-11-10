namespace MySvelteApp.Server.Features.Auth.LoginUser;

public class LoginUserRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

