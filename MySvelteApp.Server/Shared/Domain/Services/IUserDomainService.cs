using MySvelteApp.Server.Shared.Domain.Entities;
using MySvelteApp.Server.Shared.Domain.ValueObjects;

namespace MySvelteApp.Server.Shared.Domain.Services;

/// <summary>
/// Domain service for user-related business logic that spans multiple entities or requires complex operations.
/// </summary>
public interface IUserDomainService
{
    /// <summary>
    /// Validates if a user can be registered with the given username and email.
    /// </summary>
    /// <param name="username">The username to check</param>
    /// <param name="email">The email to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tuple indicating if registration is allowed and an error message if not</returns>
    Task<(bool CanRegister, string? ErrorMessage)> CanRegisterUserAsync(
        Username username,
        Email email,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new user entity with the provided information.
    /// </summary>
    /// <param name="username">The username</param>
    /// <param name="email">The email</param>
    /// <param name="passwordHash">The hashed password</param>
    /// <param name="passwordSalt">The password salt</param>
    /// <returns>A new User entity</returns>
    User CreateUser(
        Username username,
        Email email,
        string passwordHash,
        string passwordSalt);
}
