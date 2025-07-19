namespace FS.EntityFramework.Library.Domain;

/// <summary>
/// Unit of work interface for domain operations
/// Manages transaction boundaries and aggregate persistence
/// </summary>
public interface IDomainUnitOfWork : IDisposable
{
    /// <summary>
    /// Saves all pending changes to the database
    /// Publishes domain events after successful persistence
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The number of state entries written to the database</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        
    /// <summary>
    /// Begins a new transaction for atomic operations
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
        
    /// <summary>
    /// Commits the current transaction
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
        
    /// <summary>
    /// Rolls back the current transaction
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}