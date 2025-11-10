using MySvelteApp.Server.Shared.Common.Interfaces;
using MySvelteApp.Server.Shared.Domain.Entities;
using MySvelteApp.Server.Shared.Domain.ValueObjects;

namespace MySvelteApp.Server.Shared.Domain.Services;

/// <summary>
/// Domain service implementation for user-related business logic.
/// </summary>
public class UserDomainService : IUserDomainService
{
    private readonly IUserRepository _userRepository;

    public UserDomainService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<(bool CanRegister, string? ErrorMessage)> CanRegisterUserAsync(
        Username username,
        Email email,
        CancellationToken cancellationToken = default)
    {
        if (await _userRepository.UsernameExistsAsync(username, cancellationToken))
        {
            return (false, "This username is already taken. Please choose a different one.");
        }

        if (await _userRepository.EmailExistsAsync(email, cancellationToken))
        {
            return (false, "This email is already registered. Please use a different email address.");
        }

        return (true, null);
    }

    public User CreateUser(
        Username username,
        Email email,
        string passwordHash,
        string passwordSalt)
    {
        return new User
        {
            Username = username,
            Email = email,
            PasswordHash = passwordHash,
            PasswordSalt = passwordSalt
        };
    }
}
