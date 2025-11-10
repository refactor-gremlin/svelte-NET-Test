using MySvelteApp.Server.Features.Auth.LoginUser;
using MySvelteApp.Server.Shared.Common.Interfaces;
using MySvelteApp.Server.Shared.Common.Results;

namespace MySvelteApp.Server.Features.Auth.LoginUser;

public class LoginUserHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public LoginUserHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<ApiResult<LoginUserResponse>> HandleAsync(
        LoginUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var trimmedUsername = request.Username.Trim();
        var user = await _userRepository.GetByUsernameAsync(trimmedUsername, cancellationToken);

        if (user is null)
        {
            return ApiResult<LoginUserResponse>.Unauthorized("Invalid username or password. Please check your credentials and try again.");
        }

        var passwordValid = _passwordHasher.VerifyPassword(request.Password, user.PasswordHash, user.PasswordSalt);
        if (!passwordValid)
        {
            return ApiResult<LoginUserResponse>.Unauthorized("Invalid username or password. Please check your credentials and try again.");
        }

        var token = _jwtTokenGenerator.GenerateToken(user);

        return ApiResult<LoginUserResponse>.Success(new LoginUserResponse
        {
            Token = token,
            UserId = user.Id,
            Username = user.Username
        });
    }
}

