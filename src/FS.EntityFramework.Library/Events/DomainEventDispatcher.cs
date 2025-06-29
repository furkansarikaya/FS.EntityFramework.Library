using FS.EntityFramework.Library.Common;
using Microsoft.Extensions.DependencyInjection;

namespace FS.EntityFramework.Library.Events;

/// <summary>
/// Default implementation of domain event dispatcher using dependency injection
/// </summary>
public class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the DomainEventDispatcher class
    /// </summary>
    /// <param name="serviceProvider">Service provider for resolving handlers</param>
    public DomainEventDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Dispatches a single domain event to its handlers
    /// </summary>
    /// <param name="domainEvent">The domain event to dispatch</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var domainEventType = domainEvent.GetType();
        var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEventType);
        
        var handlers = _serviceProvider.GetServices(handlerType);
        
        var tasks = handlers.Select(handler =>
        {
            var method = handlerType.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.Handle));
            return (Task)method!.Invoke(handler, new object[] { domainEvent, cancellationToken })!;
        });

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Dispatches multiple domain events to their handlers
    /// </summary>
    /// <param name="domainEvents">The domain events to dispatch</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        var tasks = domainEvents.Select(domainEvent => DispatchAsync(domainEvent, cancellationToken));
        await Task.WhenAll(tasks);
    }
}