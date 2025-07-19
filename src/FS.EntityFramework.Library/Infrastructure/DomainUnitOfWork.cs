using FS.EntityFramework.Library.Common;
using FS.EntityFramework.Library.Domain;

namespace FS.EntityFramework.Library.Infrastructure;

/// <summary>
/// Domain unit of work implementation that wraps the existing EF unit of work
/// Manages transaction boundaries and domain event publishing
/// </summary>
public class DomainUnitOfWork : IDomainUnitOfWork
{
    private readonly UnitOfWorks.IUnitOfWork _efUnitOfWork;
    private readonly Events.IDomainEventDispatcher? _domainEventDispatcher;
    private readonly List<IAggregateRoot> _aggregatesWithEvents = [];
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the DomainUnitOfWork class
    /// </summary>
    /// <param name="efUnitOfWork">The underlying EF unit of work</param>
    /// <param name="domainEventDispatcher">Optional domain event dispatcher</param>
    public DomainUnitOfWork(
        UnitOfWorks.IUnitOfWork efUnitOfWork,
        Events.IDomainEventDispatcher? domainEventDispatcher = null)
    {
        _efUnitOfWork = efUnitOfWork;
        _domainEventDispatcher = domainEventDispatcher;
    }

    /// <summary>
    /// Registers an aggregate that has domain events to be published
    /// </summary>
    /// <param name="aggregate">The aggregate with domain events</param>
    public void RegisterAggregate(IAggregateRoot aggregate)
    {
        if (aggregate.DomainEvents.Count != 0 && !_aggregatesWithEvents.Contains(aggregate))
        {
            _aggregatesWithEvents.Add(aggregate);
        }
    }

    /// <summary>
    /// Saves all pending changes to the database and publishes domain events
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The number of state entries written to the database</returns>
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // 1. Collect domain events before saving
        var allDomainEvents = new List<Common.IDomainEvent>();
        foreach (var aggregate in _aggregatesWithEvents)
        {
            allDomainEvents.AddRange(aggregate.DomainEvents);
        }

        // 2. Save changes to database first
        var result = await _efUnitOfWork.SaveChangesAsync(cancellationToken);

        // 3. Publish domain events after successful save
        if (allDomainEvents.Count != 0 && _domainEventDispatcher != null)
        {
            await _domainEventDispatcher.DispatchAsync(allDomainEvents, cancellationToken);
        }

        // 4. Clear domain events from aggregates
        foreach (var aggregate in _aggregatesWithEvents)
        {
            aggregate.ClearDomainEvents();
        }

        _aggregatesWithEvents.Clear();

        return result;
    }

    /// <summary>
    /// Begins a new transaction for atomic operations
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        await _efUnitOfWork.BeginTransactionAsync();
    }

    /// <summary>
    /// Commits the current transaction
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        await _efUnitOfWork.CommitTransactionAsync();
    }

    /// <summary>
    /// Rolls back the current transaction
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        await _efUnitOfWork.RollbackTransactionAsync();
    }

    /// <summary>
    /// Releases all resources used by the DomainUnitOfWork
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the unmanaged resources and optionally releases the managed resources
    /// </summary>
    /// <param name="disposing">True to release both managed and unmanaged resources; false to release only unmanaged resources</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed || !disposing) return;
        _efUnitOfWork?.Dispose();
        _aggregatesWithEvents.Clear();
        _disposed = true;
    }
}