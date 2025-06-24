using System.Collections.Concurrent;
using FS.EntityFramework.Library.Common;
using FS.EntityFramework.Library.Interfaces;
using FS.EntityFramework.Library.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace FS.EntityFramework.Library.UnitOfWorks;

public class UnitOfWork(DbContext context, IServiceProvider serviceProvider)
    : IUnitOfWork
{
    private readonly ConcurrentDictionary<Type, object> _repositories = new();
    private IDbContextTransaction? _currentTransaction;
    private bool _disposed;
    
    // ===== GENERIC REPOSITORY ACCESS =====
    
    /// <summary>
    /// Get specific repository implementation by type.
    /// Bu method custom repository interface'lerinizi resolve ediyor.
    /// </summary>
    public TRepository GetRepository<TRepository>() where TRepository : class
    {
        var repositoryType = typeof(TRepository);
        
        return (TRepository)_repositories.GetOrAdd(repositoryType, _ =>
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
    /// Get generic repository for entities without specific repository.
    /// Bu method generic IRepository<TEntity, TKey> access sağlıyor.
    /// </summary>
    public IRepository<TEntity, TKey> GetRepository<TEntity, TKey>() 
        where TEntity : BaseEntity<TKey>
        where TKey : IEquatable<TKey>
    {
        var repositoryType = typeof(IRepository<TEntity, TKey>);
        
        return (IRepository<TEntity, TKey>)_repositories.GetOrAdd(repositoryType, _ =>
        {
            // Try to get specific repository first
            var specificRepository = serviceProvider.GetService<IRepository<TEntity, TKey>>();
            if (specificRepository != null)
            {
                return specificRepository;
            }
            
            // Create generic repository if no specific implementation
            return new BaseRepository<TEntity, TKey>(context);
        });
    }

    public IRepository<TEntity, TKey> Repository<TEntity, TKey>() 
        where TEntity : BaseEntity<TKey> 
        where TKey : IEquatable<TKey>
    {
        var type = typeof(TEntity);
        
        if (!_repositories.ContainsKey(type))
        {
            _repositories[type] = new BaseRepository<TEntity, TKey>(context);
        }
        
        return (IRepository<TEntity, TKey>)_repositories[type];
    }
    
    // ===== PERSISTENCE OPERATIONS =====
    
    /// <summary>
    /// Save all changes across all repositories.
    /// BURADA tüm repository changes tek seferde persist ediliyor.
    /// </summary>
    public async Task<int> SaveChangesAsync()
    {
        return await context.SaveChangesAsync();
    }
    
    /// <summary>
    /// Save all changes with cancellation token.
    /// </summary>
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
    /// Get entity entry for state management.
    /// </summary>
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
    
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed || !disposing) return;
        _currentTransaction?.Dispose();
        context.Dispose();
        _repositories.Clear();
        _disposed = true;
    }
}