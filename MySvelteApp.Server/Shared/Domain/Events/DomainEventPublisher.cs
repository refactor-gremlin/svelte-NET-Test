using Microsoft.Extensions.Logging;

namespace MySvelteApp.Server.Shared.Domain.Events;

/// <summary>
/// Simple in-memory domain event publisher implementation.
/// Events are published synchronously to registered handlers.
/// For production use, consider using MediatR or a message broker.
/// </summary>
public class DomainEventPublisher : IDomainEventPublisher
{
    private readonly ILogger<DomainEventPublisher> _logger;
    private readonly IServiceProvider _serviceProvider;

    public DomainEventPublisher(
        IServiceProvider serviceProvider,
        ILogger<DomainEventPublisher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task PublishAsync<T>(T domainEvent, CancellationToken cancellationToken = default) where T : IDomainEvent
    {
        _logger.LogInformation("Publishing domain event: {EventType} at {OccurredAt}", 
            typeof(T).Name, domainEvent.OccurredAt);

        // Look for handlers that implement IDomainEventHandler<T>
        var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(typeof(T));
        var handlers = _serviceProvider.GetServices(handlerType);

        foreach (var handler in handlers)
        {
            try
            {
                if (handler is IDomainEventHandler<T> typedHandler)
                {
                    await typedHandler.HandleAsync(domainEvent, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling domain event {EventType}", typeof(T).Name);
                // Continue processing other handlers even if one fails
            }
        }
    }
}
