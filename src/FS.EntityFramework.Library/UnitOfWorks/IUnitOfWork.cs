using FS.EntityFramework.Library.Common;
using FS.EntityFramework.Library.Interfaces;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;

namespace FS.EntityFramework.Library.UnitOfWorks;

public interface IUnitOfWork : IDisposable
{
    // ===== GENERIC REPOSITORY ACCESS =====
    // Generic repository access for entities without specific repositories
    TRepository GetRepository<TRepository>() where TRepository : class;
    IRepository<TEntity, TKey> GetRepository<TEntity, TKey>() 
        where TEntity : BaseEntity<TKey>
        where TKey : IEquatable<TKey>;
    
    // ===== PERSISTENCE OPERATIONS =====
    // Save changes across all repositories
    Task<int> SaveChangesAsync();
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    
    // ===== TRANSACTION MANAGEMENT =====
    // Transaction control methods
    Task<IDbContextTransaction> BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
    
    // Bulk operations across multiple repositories
    Task<TKey> ExecuteInTransactionAsync<TKey>(Func<Task<TKey>> operation) 
        where TKey : IEquatable<TKey>;
    
    // ===== CHANGE TRACKING =====
    // Entity state management
    bool HasChanges { get; }
    void DetachAllEntities();
    EntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class;
}