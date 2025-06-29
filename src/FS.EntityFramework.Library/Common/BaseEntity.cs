namespace FS.EntityFramework.Library.Common;

/// <summary>
/// Base entity class that provides the primary key property and optional domain events support
/// </summary>
/// <typeparam name="TKey">The type of the entity's primary key</typeparam>
public abstract class BaseEntity<TKey> : IEntity<TKey>, IHasDomainEvents where TKey : IEquatable<TKey>
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// Gets or sets the primary key of the entity
    /// </summary>
    public TKey Id { get; set; } = default!;

    /// <summary>
    /// Gets the collection of domain events raised by this entity
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Adds a domain event to the entity
    /// </summary>
    /// <param name="domainEvent">The domain event to add</param>
    public void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Removes a domain event from the entity
    /// </summary>
    /// <param name="domainEvent">The domain event to remove</param>
    public void RemoveDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Remove(domainEvent);
    }

    /// <summary>
    /// Clears all domain events from the entity
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
