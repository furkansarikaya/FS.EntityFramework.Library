using FS.EntityFramework.Library.Common;

namespace FS.EntityFramework.Library.Events;

/// <summary>
/// Interface for dispatching domain events to their respective handlers
/// </summary>
public interface IDomainEventDispatcher
{
    /// <summary>
    /// Dispatches a single domain event to its handlers
    /// </summary>
    /// <param name="domainEvent">The domain event to dispatch</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Dispatches multiple domain events to their handlers
    /// </summary>
    /// <param name="domainEvents">The domain events to dispatch</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}