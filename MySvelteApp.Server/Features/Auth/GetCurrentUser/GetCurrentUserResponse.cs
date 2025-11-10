using MySvelteApp.Server.Shared.Common.DTOs;

namespace MySvelteApp.Server.Features.Auth.GetCurrentUser;

public class GetCurrentUserResponse
{
    public UserDto User { get; set; } = new();
}

