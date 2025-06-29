using FS.EntityFramework.Library.Common;

namespace FS.EntityFramework.Library.Events;

/// <summary>
/// Generic interface for handling domain events
/// </summary>
/// <typeparam name="TDomainEvent">The type of domain event to handle</typeparam>
public interface IDomainEventHandler<in TDomainEvent> where TDomainEvent : IDomainEvent
{
    /// <summary>
    /// Handles the specified domain event
    /// </summary>
    /// <param name="domainEvent">The domain event to handle</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task Handle(TDomainEvent domainEvent, CancellationToken cancellationToken = default);
}