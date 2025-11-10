using MySvelteApp.Server.Features.Auth.RegisterUser;
using MySvelteApp.Server.Shared.Common.Interfaces;
using MySvelteApp.Server.Shared.Common.Results;
using MySvelteApp.Server.Shared.Domain.Entities;

namespace MySvelteApp.Server.Features.Auth.RegisterUser;

public class RegisterUserHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterUserHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResult<RegisterUserResponse>> HandleAsync(
        RegisterUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var trimmedUsername = request.Username.Trim();
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        if (await _userRepository.UsernameExistsAsync(trimmedUsername, cancellationToken))
        {
            return ApiResult<RegisterUserResponse>.Conflict("This username is already taken. Please choose a different one.");
        }

        if (await _userRepository.EmailExistsAsync(normalizedEmail, cancellationToken))
        {
            return ApiResult<RegisterUserResponse>.Conflict("This email is already registered. Please use a different email address.");
        }

        var (hash, salt) = _passwordHasher.HashPassword(request.Password);

        var user = new User
        {
            Username = trimmedUsername,
            Email = normalizedEmail,
            PasswordHash = hash,
            PasswordSalt = salt
        };

        await _userRepository.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var token = _jwtTokenGenerator.GenerateToken(user);

        return ApiResult<RegisterUserResponse>.Success(new RegisterUserResponse
        {
            Token = token,
            UserId = user.Id,
            Username = user.Username
        });
    }
}

