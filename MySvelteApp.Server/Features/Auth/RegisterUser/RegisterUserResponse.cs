namespace MySvelteApp.Server.Features.Auth.RegisterUser;

public class RegisterUserResponse
{
    public string Token { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
}

