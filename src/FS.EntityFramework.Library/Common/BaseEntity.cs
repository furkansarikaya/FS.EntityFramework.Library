namespace FS.EntityFramework.Library.Common;

/// <summary>
/// Base entity class that provides the primary key property and optional domain events support
/// </summary>
/// <typeparam name="TKey">The type of the entity's primary key</typeparam>
public abstract class BaseEntity<TKey> : IEntity<TKey>, IHasDomainEvents where TKey : IEquatable<TKey>
{
    private readonly List<IDomainEvent> _domainEvents = [];
    
    /// <summary>
    /// Initializes a new instance with default ID value.
    /// ID generation is delegated to registered IIdGenerator implementations.
    /// </summary>
    protected BaseEntity()
    {
        Id = default!;
    }

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
    
    /// <summary>
    /// Determines whether the specified object is equal to the current entity
    /// </summary>
    /// <param name="obj">The object to compare with the current entity</param>
    /// <returns>True if the specified object is equal to the current entity; otherwise, false</returns>
    public override bool Equals(object? obj)
    {
        if (obj is not BaseEntity<TKey> other)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        return GetType() == other.GetType() && Id.Equals(other.Id);
    }

    /// <summary>
    /// Gets the hash code for the current entity
    /// </summary>
    /// <returns>A hash code for the current entity</returns>
    public override int GetHashCode()
    {
        return (GetType().Name + Id).GetHashCode();
    }

    /// <summary>
    /// Determines whether two entities are equal
    /// </summary>
    /// <param name="left">The left entity</param>
    /// <param name="right">The right entity</param>
    /// <returns>True if the entities are equal; otherwise, false</returns>
    public static bool operator ==(BaseEntity<TKey>? left, BaseEntity<TKey>? right)
    {
        return left?.Equals(right) ?? ReferenceEquals(right, null);
    }

    /// <summary>
    /// Determines whether two entities are not equal
    /// </summary>
    /// <param name="left">The left entity</param>
    /// <param name="right">The right entity</param>
    /// <returns>True if the entities are not equal; otherwise, false</returns>
    public static bool operator !=(BaseEntity<TKey>? left, BaseEntity<TKey>? right)
    {
        return !(left == right);
    }
}
