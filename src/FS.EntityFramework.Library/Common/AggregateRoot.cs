using FS.EntityFramework.Library.Domain;

namespace FS.EntityFramework.Library.Common;

/// <summary>
/// Enhanced base class for Aggregate Roots with proper DDD implementation
/// Provides domain event management, versioning, and consistency enforcement
/// </summary>
/// <typeparam name="TKey">The type of the aggregate root identifier</typeparam>
public abstract class AggregateRoot<TKey> : Common.BaseEntity<TKey>, IAggregateRoot
    where TKey : IEquatable<TKey>
{
    private long _version = 1;

    /// <summary>
    /// Initializes a new instance of the AggregateRoot class
    /// </summary>
    protected AggregateRoot()
    {
    }

    /// <summary>
    /// Initializes a new instance of the AggregateRoot class with specified identifier
    /// </summary>
    /// <param name="id">The aggregate root identifier</param>
    protected AggregateRoot(TKey id)
    {
        Id = id;
    }

    /// <summary>
    /// Gets the version of this aggregate for optimistic concurrency control
    /// </summary>
    public virtual long Version
    {
        get => _version;
        protected set => _version = value;
    }

    /// <summary>
    /// Increments the version of this aggregate
    /// Should be called when the aggregate state changes significantly
    /// </summary>
    protected void IncrementVersion()
    {
        _version++;
    }

    /// <summary>
    /// Raises a domain event from this aggregate
    /// The event will be published when the aggregate is saved to ensure consistency
    /// </summary>
    /// <param name="domainEvent">The domain event to raise</param>
    protected void RaiseDomainEvent(Common.IDomainEvent domainEvent)
    {
        AddDomainEvent(domainEvent);
        IncrementVersion();
    }

    /// <summary>
    /// Checks business rules and throws exception if any are violated
    /// This method should be called before any state-changing operations
    /// </summary>
    /// <param name="rules">The business rules to check</param>
    protected void CheckRule(params IBusinessRule[] rules)
    {
        foreach (var rule in rules)
        {
            if (rule.IsBroken())
            {
                throw new BusinessRuleValidationException(rule);
            }
        }
    }
}

/// <summary>
/// Convenience base class for Aggregate Roots with Guid identifiers
/// Most common use case for aggregate root identification
/// </summary>
public abstract class AggregateRoot : AggregateRoot<Guid>
{
    /// <summary>
    /// Initializes a new instance with a new Guid identifier
    /// </summary>
    protected AggregateRoot() : base(Guid.CreateVersion7())
    {
    }

    /// <summary>
    /// Initializes a new instance with the specified Guid identifier
    /// </summary>
    /// <param name="id">The Guid identifier</param>
    protected AggregateRoot(Guid id) : base(id)
    {
    }
}