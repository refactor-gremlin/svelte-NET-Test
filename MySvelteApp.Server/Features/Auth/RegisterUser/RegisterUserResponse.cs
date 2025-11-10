using MySvelteApp.Server.Shared.Common.DTOs;

namespace MySvelteApp.Server.Features.Auth.RegisterUser;

public class RegisterUserResponse
{
    public string Token { get; set; } = string.Empty;
    public UserDto User { get; set; } = null!;
}

