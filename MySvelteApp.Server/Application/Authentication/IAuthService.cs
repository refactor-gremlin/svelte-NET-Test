using MySvelteApp.Server.Application.Authentication.DTOs;

namespace MySvelteApp.Server.Application.Authentication;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<CurrentUserResponse?> GetCurrentUserAsync(int userId, CancellationToken cancellationToken = default);
}
