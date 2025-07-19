using System.Collections.Concurrent;
using FS.EntityFramework.Library.Common;
using FS.EntityFramework.Library.Interfaces;
using FS.EntityFramework.Library.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace FS.EntityFramework.Library.UnitOfWorks;

/// <summary>
/// Implementation of the Unit of Work pattern that coordinates multiple repositories
/// and manages database transactions and change tracking
/// </summary>
/// <param name="context">The Entity Framework context</param>
/// <param name="serviceProvider">The service provider for dependency injection</param>
public class UnitOfWork(DbContext context, IServiceProvider serviceProvider)
    : IUnitOfWork
{
    /// <summary>
    /// Thread-safe dictionary to cache repository instances
    /// </summary>
    private readonly ConcurrentDictionary<string, object> _repositories = new();
    
    /// <summary>
    /// The current database transaction (if any)
    /// </summary>
    private IDbContextTransaction? _currentTransaction;
    
    /// <summary>
    /// Flag indicating whether the object has been disposed
    /// </summary>
    private bool _disposed;
    
    // ===== GENERIC REPOSITORY ACCESS =====
    
    /// <summary>
    /// Gets a specific repository implementation by type from the service container
    /// </summary>
    /// <typeparam name="TRepository">The repository type to resolve</typeparam>
    /// <returns>The repository instance</returns>
    public TRepository GetRepository<TRepository>() where TRepository : class
    {
        var repositoryType = typeof(TRepository);
        
        // For specific repository types, we can safely use the type name as key
        // because these are concrete implementations, not generic interfaces
        var cacheKey = repositoryType.FullName ?? repositoryType.Name;
        
        return (TRepository)_repositories.GetOrAdd(cacheKey, _ =>
        {
            var repository = serviceProvider.GetRequiredService<TRepository>();
            if (repository == null)
            {
                throw new InvalidOperationException($"Repository of type {repositoryType.Name} is not registered");
            }
            return repository;
        });
    }
    
    /// <summary>
    /// Gets a generic repository for the specified entity type
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <typeparam name="TKey">The entity's primary key type</typeparam>
    /// <returns>A repository instance for the entity</returns>
    public IRepository<TEntity, TKey> GetRepository<TEntity, TKey>() 
        where TEntity : BaseEntity<TKey>
        where TKey : IEquatable<TKey>
    {
        // CRITICAL FIX: Create composite key that uniquely identifies entity + key combination
        // This prevents different entities with same key type from sharing repository instances
        var cacheKey = CreateRepositoryCacheKey<TEntity, TKey>();
        
        return (IRepository<TEntity, TKey>)_repositories.GetOrAdd(cacheKey, _ =>
        {
            // Try to get specific repository first
            var specificRepository = serviceProvider.GetService<IRepository<TEntity, TKey>>();
            return specificRepository ??
                   // Create generic repository if no specific implementation
                   new BaseRepository<TEntity, TKey>(context);
        });
    }

    /// <summary>
    /// Gets a generic repository for entities without specific repository implementation.
    /// This method provides access to IRepository&lt;TEntity, TKey&gt;.
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <typeparam name="TKey">The entity's primary key type</typeparam>
    /// <returns>A repository instance for the entity</returns>
    public IRepository<TEntity, TKey> Repository<TEntity, TKey>() 
        where TEntity : BaseEntity<TKey> 
        where TKey : IEquatable<TKey>
    {
        // Use the same caching strategy for consistency
        return GetRepository<TEntity, TKey>();
    }
    
    /// <summary>
    /// Creates a unique cache key for repository instances that prevents type collisions
    /// This method ensures that entities with the same key type get different repository instances
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <typeparam name="TKey">The entity's primary key type</typeparam>
    /// <returns>A unique cache key string</returns>
    private static string CreateRepositoryCacheKey<TEntity, TKey>()
        where TEntity : BaseEntity<TKey>
        where TKey : IEquatable<TKey>
    {
        var entityType = typeof(TEntity);
        var keyType = typeof(TKey);
        
        // Create composite key format: "IRepository<EntityFullName,KeyFullName>"
        // Example: "IRepository<MyApp.Entities.Product,System.Int32>"
        // This ensures Product<int> and Category<int> get different cache keys
        return $"IRepository<{entityType.FullName},{keyType.FullName}>";
    }
    
    // ===== PERSISTENCE OPERATIONS =====
    
    /// <summary>
    /// Saves all pending changes across all repositories to the database
    /// </summary>
    /// <returns>The number of state entries written to the database</returns>
    public async Task<int> SaveChangesAsync()
    {
        return await context.SaveChangesAsync();
    }
    
    /// <summary>
    /// Saves all pending changes across all repositories to the database
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The number of state entries written to the database</returns>
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return await context.SaveChangesAsync(cancellationToken);
    }
    
    // ===== TRANSACTION MANAGEMENT =====
    /// <summary>
    /// Begin database transaction.
    /// Bu method explicit transaction control sağlıyor.
    /// </summary>
    public async Task<IDbContextTransaction> BeginTransactionAsync()
    {
        if (_currentTransaction != null)
        {
            throw new InvalidOperationException("A transaction is already in progress");
        }
        
        _currentTransaction = await context.Database.BeginTransactionAsync();
        return _currentTransaction;
    }
    
    /// <summary>
    /// Commit current transaction.
    /// </summary>
    public async Task CommitTransactionAsync()
    {
        if (_currentTransaction == null)
        {
            throw new InvalidOperationException("No transaction in progress");
        }
        
        try
        {
            await SaveChangesAsync();
            await _currentTransaction.CommitAsync();
        }
        catch
        {
            await RollbackTransactionAsync();
            throw;
        }
        finally
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }
    
    /// <summary>
    /// Rollback current transaction.
    /// </summary>
    public async Task RollbackTransactionAsync()
    {
        if (_currentTransaction == null)
        {
            throw new InvalidOperationException("No transaction in progress");
        }
        
        try
        {
            await _currentTransaction.RollbackAsync();
        }
        finally
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    /// <summary>
    /// Execute operation in transaction.
    /// </summary>
    public async Task<TKey> ExecuteInTransactionAsync<TKey>(Func<Task<TKey>> operation) where TKey : IEquatable<TKey>
    {
        await BeginTransactionAsync();
        try
        {
            var result = await operation();
            await CommitTransactionAsync();
            return result;
        }
        catch
        {
            await RollbackTransactionAsync();
            throw;
        }
    }
    
    // ===== CHANGE TRACKING =====
    
    /// <summary>
    /// Check if context has pending changes.
    /// </summary>
    public bool HasChanges => context.ChangeTracker.HasChanges();
    
    /// <summary>
    /// Detach all tracked entities.
    /// </summary>
    public void DetachAllEntities()
    {
        var entries = context.ChangeTracker.Entries().ToList();
        foreach (var entry in entries)
        {
            entry.State = EntityState.Detached;
        }
    }
    
    /// <summary>
    /// Gets the entity entry for the specified entity for state management
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <param name="entity">The entity instance</param>
    /// <returns>The entity entry</returns>
    public EntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class
    {
        return context.Entry(entity);
    }
    
    // ===== DISPOSAL =====
    
    /// <summary>
    /// Dispose UnitOfWork and cleanup resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases all resources used by the UnitOfWork
    /// </summary>
    /// <param name="disposing">True if disposing; otherwise, false</param>
    private void Dispose(bool disposing)
    {
        if (_disposed || !disposing) return;
        _currentTransaction?.Dispose();
        context.Dispose();
        _repositories.Clear();
        _disposed = true;
    }
}