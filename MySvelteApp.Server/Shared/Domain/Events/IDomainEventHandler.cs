namespace MySvelteApp.Server.Shared.Domain.Events;

/// <summary>
/// Handler interface for domain events.
/// Implement this interface to handle specific domain events.
/// </summary>
/// <typeparam name="T">The type of domain event to handle</typeparam>
public interface IDomainEventHandler<in T> where T : IDomainEvent
{
    /// <summary>
    /// Handles the domain event.
    /// </summary>
    /// <param name="domainEvent">The domain event to handle</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task HandleAsync(T domainEvent, CancellationToken cancellationToken = default);
}
