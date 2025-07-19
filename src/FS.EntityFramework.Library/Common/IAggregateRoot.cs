namespace FS.EntityFramework.Library.Common;

/// <summary>
/// Enhanced Aggregate Root interface for proper DDD implementation
/// Aggregate Roots are the only entities that can be directly accessed from outside the aggregate
/// They ensure consistency boundaries and manage domain events
/// </summary>
public interface IAggregateRoot
{
    /// <summary>
    /// Gets the collection of domain events raised by this aggregate
    /// Events are published when the aggregate is persisted
    /// </summary>
    IReadOnlyCollection<Common.IDomainEvent> DomainEvents { get; }
        
    /// <summary>
    /// Clears all domain events from this aggregate
    /// This is typically called after events have been published
    /// </summary>
    void ClearDomainEvents();
        
    /// <summary>
    /// Gets the version of this aggregate for optimistic concurrency control
    /// Helps prevent concurrent modification conflicts
    /// </summary>
    long Version { get; }
}