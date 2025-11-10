using MySvelteApp.Server.Features.Auth.GetCurrentUser;
using MySvelteApp.Server.Shared.Common.Interfaces;
using MySvelteApp.Server.Shared.Common.Results;
using MySvelteApp.Server.Shared.Common.DTOs;

namespace MySvelteApp.Server.Features.Auth.GetCurrentUser;

public class GetCurrentUserHandler
{
    private readonly IUserRepository _userRepository;

    public GetCurrentUserHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<ApiResult<GetCurrentUserResponse>> HandleAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return ApiResult<GetCurrentUserResponse>.NotFound("User not found.");
        }

        return ApiResult<GetCurrentUserResponse>.Success(new GetCurrentUserResponse
        {
            User = new UserDto
            {
                Id = user.Id,
                Username = user.Username.Value,
                Email = user.Email.Value
            }
        });
    }
}

