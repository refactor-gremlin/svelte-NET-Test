using MySvelteApp.Server.Application.Authentication.DTOs;
using MySvelteApp.Server.Application.Common.Interfaces;
using MySvelteApp.Server.Domain.Entities;

namespace MySvelteApp.Server.Application.Authentication;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IUnitOfWork _unitOfWork;

    public AuthService(
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

    public async Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var trimmedUsername = request.Username.Trim();
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        if (await _userRepository.UsernameExistsAsync(trimmedUsername, cancellationToken))
        {
            return CreateError("This username is already taken. Please choose a different one.", AuthErrorType.Conflict);
        }

        if (await _userRepository.EmailExistsAsync(normalizedEmail, cancellationToken))
        {
            return CreateError("This email is already registered. Please use a different email address.", AuthErrorType.Conflict);
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

        return new AuthResult
        {
            Success = true,
            Token = token,
            UserId = user.Id,
            Username = user.Username
        };
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var trimmedUsername = request.Username.Trim();
        var user = await _userRepository.GetByUsernameAsync(trimmedUsername, cancellationToken);

        if (user is null)
        {
            return CreateUnauthorized();
        }

        var passwordValid = _passwordHasher.VerifyPassword(request.Password, user.PasswordHash, user.PasswordSalt);
        if (!passwordValid)
        {
            return CreateUnauthorized();
        }

        var token = _jwtTokenGenerator.GenerateToken(user);

        return new AuthResult
        {
            Success = true,
            Token = token,
            UserId = user.Id,
            Username = user.Username
        };
    }

    public async Task<CurrentUserResponse?> GetCurrentUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        return new CurrentUserResponse
        {
            User = new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email
            }
        };
    }

    private static AuthResult CreateError(string message, AuthErrorType errorType)
    {
        return new AuthResult
        {
            Success = false,
            ErrorMessage = message,
            ErrorType = errorType
        };
    }

    private static AuthResult CreateUnauthorized()
    {
        return new AuthResult
        {
            Success = false,
            ErrorMessage = "Invalid username or password. Please check your credentials and try again.",
            ErrorType = AuthErrorType.Unauthorized
        };
    }
}
