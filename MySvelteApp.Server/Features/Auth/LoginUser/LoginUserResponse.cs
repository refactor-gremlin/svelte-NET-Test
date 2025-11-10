using MySvelteApp.Server.Shared.Common.DTOs;

namespace MySvelteApp.Server.Features.Auth.LoginUser;

public class LoginUserResponse
{
    public string Token { get; set; } = string.Empty;
    public UserDto User { get; set; } = null!;
}

