namespace MySvelteApp.Server.Features.Auth.RegisterUser;

using MySvelteApp.Server.Shared.Domain.Events;

/// <summary>
/// Domain event raised when a user is successfully registered.
/// </summary>
public record UserRegisteredEvent(int UserId, string Username, string Email) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
