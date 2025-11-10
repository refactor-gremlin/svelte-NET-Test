namespace MySvelteApp.Server.Features.Auth.LoginUser;

public class LoginUserResponse
{
    public string Token { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
}

