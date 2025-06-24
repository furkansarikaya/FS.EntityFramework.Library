using FS.EntityFramework.Library.Common;
using FS.EntityFramework.Library.Interfaces;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;

namespace FS.EntityFramework.Library.UnitOfWorks;

/// <summary>
/// Represents a unit of work pattern implementation that coordinates multiple repositories
/// and manages database transactions and change tracking
/// </summary>
public interface IUnitOfWork : IDisposable
{
    // ===== GENERIC REPOSITORY ACCESS =====
    // Generic repository access for entities without specific repositories
    
    /// <summary>
    /// Gets a specific repository implementation by type from the service container
    /// </summary>
    /// <typeparam name="TRepository">The repository type to resolve</typeparam>
    /// <returns>The repository instance</returns>
    TRepository GetRepository<TRepository>() where TRepository : class;
    
    /// <summary>
    /// Gets a generic repository for the specified entity type
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <typeparam name="TKey">The entity's primary key type</typeparam>
    /// <returns>A repository instance for the entity</returns>
    IRepository<TEntity, TKey> GetRepository<TEntity, TKey>() 
        where TEntity : BaseEntity<TKey>
        where TKey : IEquatable<TKey>;
    
    // ===== PERSISTENCE OPERATIONS =====
    // Save changes across all repositories
    
    /// <summary>
    /// Saves all pending changes across all repositories to the database
    /// </summary>
    /// <returns>The number of state entries written to the database</returns>
    Task<int> SaveChangesAsync();
    
    /// <summary>
    /// Saves all pending changes across all repositories to the database
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The number of state entries written to the database</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    
    // ===== TRANSACTION MANAGEMENT =====
    // Transaction control methods
    
    /// <summary>
    /// Begins a new database transaction
    /// </summary>
    /// <returns>The database transaction</returns>
    Task<IDbContextTransaction> BeginTransactionAsync();
    
    /// <summary>
    /// Commits the current database transaction
    /// </summary>
    Task CommitTransactionAsync();
    
    /// <summary>
    /// Rolls back the current database transaction
    /// </summary>
    Task RollbackTransactionAsync();
    
    // Bulk operations across multiple repositories
    
    /// <summary>
    /// Executes an operation within a database transaction
    /// </summary>
    /// <typeparam name="TKey">The return type of the operation</typeparam>
    /// <param name="operation">The operation to execute</param>
    /// <returns>The result of the operation</returns>
    Task<TKey> ExecuteInTransactionAsync<TKey>(Func<Task<TKey>> operation) 
        where TKey : IEquatable<TKey>;
    
    // ===== CHANGE TRACKING =====
    // Entity state management
    
    /// <summary>
    /// Gets a value indicating whether the context has pending changes
    /// </summary>
    bool HasChanges { get; }
    
    /// <summary>
    /// Detaches all tracked entities from the change tracker
    /// </summary>
    void DetachAllEntities();
    
    /// <summary>
    /// Gets the entity entry for the specified entity for state management
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <param name="entity">The entity instance</param>
    /// <returns>The entity entry</returns>
    EntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class;
}