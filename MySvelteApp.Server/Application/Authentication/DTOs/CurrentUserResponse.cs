namespace MySvelteApp.Server.Application.Authentication.DTOs;

public class CurrentUserResponse
{
    public UserDto User { get; set; } = new();
}
