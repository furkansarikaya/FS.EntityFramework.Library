using System.Linq.Expressions;
using FS.EntityFramework.Library.Common;
using FS.EntityFramework.Library.Models;
using FS.EntityFramework.Library.Paging;
using FS.EntityFramework.Library.Specifications;

namespace FS.EntityFramework.Library.Interfaces;

/// <summary>
/// Generic repository interface for entities that provides common CRUD operations,
/// advanced querying capabilities, and support for specification pattern.
/// </summary>
/// <typeparam name="TEntity">The entity type that implements IEntity</typeparam>
/// <typeparam name="TKey">The type of the entity's primary key</typeparam>
public interface IRepository<TEntity, in TKey> 
    where TEntity : IEntity<TKey>
    where TKey : IEquatable<TKey>
{
    // Temel CRUD Operasyonları
    
    /// <summary>
    /// Gets an entity by its primary key
    /// </summary>
    /// <param name="id">The primary key value</param>
    /// <param name="disableTracking">Whether to disable change tracking</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The entity if found, otherwise null</returns>
    Task<TEntity?> GetByIdAsync(TKey id, bool disableTracking = false, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all entities from the repository
    /// </summary>
    /// <param name="disableTracking">Whether to disable change tracking</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A read-only list of all entities</returns>
    Task<IReadOnlyList<TEntity>> GetAllAsync(bool disableTracking = true, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Adds a new entity to the repository
    /// </summary>
    /// <param name="entity">The entity to add</param>
    /// <param name="saveChanges">Whether to immediately save changes to the database</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The added entity</returns>
    Task<TEntity> AddAsync(TEntity entity, bool saveChanges = false, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates an existing entity in the repository
    /// </summary>
    /// <param name="entity">The entity to update</param>
    /// <param name="saveChanges">Whether to immediately save changes to the database</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpdateAsync(TEntity entity, bool saveChanges = false, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deletes an entity from the repository
    /// </summary>
    /// <param name="entity">The entity to delete</param>
    /// <param name="saveChanges">Whether to immediately save changes to the database</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteAsync(TEntity entity, bool saveChanges = false, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deletes an entity by its primary key
    /// </summary>
    /// <param name="id">The primary key of the entity to delete</param>
    /// <param name="saveChanges">Whether to immediately save changes to the database</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteAsync(TKey id, bool saveChanges = false, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Restores a soft deleted entity (only if entity implements ISoftDelete)
    /// </summary>
    /// <param name="entity">The entity to restore</param>
    /// <param name="saveChanges">Whether to immediately save changes to the database</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RestoreAsync(TEntity entity, bool saveChanges = false, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Restores a soft deleted entity by its primary key (only if entity implements ISoftDelete)
    /// </summary>
    /// <param name="id">The primary key of the entity to restore</param>
    /// <param name="saveChanges">Whether to immediately save changes to the database</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RestoreAsync(TKey id, bool saveChanges = false, CancellationToken cancellationToken = default);
    
    // Bulk Operations
    
    /// <summary>
    /// Adds multiple entities to the repository in a single operation
    /// </summary>
    /// <param name="entities">The entities to add</param>
    /// <param name="saveChanges">Whether to immediately save changes to the database</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task BulkInsertAsync(IEnumerable<TEntity> entities, bool saveChanges = false, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates multiple entities in the repository in a single operation
    /// </summary>
    /// <param name="entities">The entities to update</param>
    /// <param name="saveChanges">Whether to immediately save changes to the database</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task BulkUpdate(IEnumerable<TEntity> entities, bool saveChanges = false, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deletes multiple entities matching the predicate in a single operation
    /// </summary>
    /// <param name="predicate">The predicate to filter entities for deletion</param>
    /// <param name="saveChanges">Whether to immediately save changes to the database</param>
    /// <param name="isSoftDelete">Whether to perform a soft delete (if entities support it)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task BulkDeleteAsync(Expression<Func<TEntity, bool>> predicate, bool saveChanges = false, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Saves all pending changes to the database
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The number of state entries written to the database</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    // Gelişmiş Sorgulama
    
    /// <summary>
    /// Gets entities with string-based include for related data
    /// </summary>
    /// <param name="predicate">Filter predicate</param>
    /// <param name="orderBy">Ordering function</param>
    /// <param name="includeString">Include string for related data</param>
    /// <param name="disableTracking">Whether to disable change tracking</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A read-only list of entities</returns>
    Task<IReadOnlyList<TEntity>> GetWithIncludeStringAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        string? includeString = null,
        bool disableTracking = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets entities with expression-based includes for related data
    /// </summary>
    /// <param name="predicate">Filter predicate</param>
    /// <param name="orderBy">Ordering function</param>
    /// <param name="includes">Include expressions for related data</param>
    /// <param name="disableTracking">Whether to disable change tracking</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A read-only list of entities</returns>
    Task<IReadOnlyList<TEntity>> GetWithIncludesAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        List<Expression<Func<TEntity, object>>>? includes = null,
        bool disableTracking = true,
        CancellationToken cancellationToken = default);
    
    // Specification pattern kullanarak sorgulama
    
    /// <summary>
    /// Gets entities using the specification pattern
    /// </summary>
    /// <param name="spec">The specification containing query criteria</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A read-only list of entities matching the specification</returns>
    Task<IReadOnlyList<TEntity>> GetAsync(BaseSpecification<TEntity> spec,
        CancellationToken cancellationToken = default);
    
    // Sayfalama için
    
    /// <summary>
    /// Gets a paginated list of entities
    /// </summary>
    /// <param name="pageIndex">The page index (1-based)</param>
    /// <param name="pageSize">The number of items per page</param>
    /// <param name="predicate">Filter predicate</param>
    /// <param name="orderBy">Ordering function</param>
    /// <param name="includes">Include expressions for related data</param>
    /// <param name="disableTracking">Whether to disable change tracking</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A paginated result containing entities and pagination metadata</returns>
    Task<IPaginate<TEntity>> GetPagedAsync(
        int pageIndex, 
        int pageSize,
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        List<Expression<Func<TEntity, object>>>? includes = null,
        bool disableTracking = true,
        CancellationToken cancellationToken = default);
    
    // Dinamik Filtreleme
    
    /// <summary>
    /// Gets a paginated list of entities with dynamic filtering
    /// </summary>
    /// <param name="filter">The filter model for dynamic filtering</param>
    /// <param name="pageIndex">The page index (1-based)</param>
    /// <param name="pageSize">The number of items per page</param>
    /// <param name="orderBy">Ordering function</param>
    /// <param name="includes">Include expressions for related data</param>
    /// <param name="disableTracking">Whether to disable change tracking</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A paginated result containing filtered entities and pagination metadata</returns>
    Task<IPaginate<TEntity>> GetPagedWithFilterAsync(
        FilterModel filter,
        int pageIndex,
        int pageSize,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        List<Expression<Func<TEntity, object>>>? includes = null,
        bool disableTracking = true,
        CancellationToken cancellationToken = default);
    
    // Advanced Query Operations
    
    /// <summary>
    /// Gets the first entity matching the predicate or null if not found
    /// </summary>
    /// <param name="predicate">The predicate to match</param>
    /// <param name="disableTracking">Whether to disable change tracking</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The first matching entity or null</returns>
    Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, bool disableTracking = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds entities matching the predicate with optional ordering and includes
    /// </summary>
    /// <param name="predicate">The predicate to match</param>
    /// <param name="orderBy">Ordering function</param>
    /// <param name="includes">Include expressions for related data</param>
    /// <param name="disableTracking">Whether to disable change tracking</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>An enumerable of matching entities</returns>
    Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        List<Expression<Func<TEntity, object>>>? includes = null,
        bool disableTracking = true,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if any entity matches the predicate
    /// </summary>
    /// <param name="predicate">The predicate to match</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if any entity matches the predicate; otherwise, false</returns>
    Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the count of entities matching the predicate
    /// </summary>
    /// <param name="predicate">The predicate to match (optional, counts all if null)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The count of matching entities</returns>
    Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default);
    
    // Raw IQueryable (ileri düzey LINQ sorguları için)
    
    /// <summary>
    /// Gets the raw IQueryable for advanced LINQ operations
    /// </summary>
    /// <param name="disableTracking">Whether to disable change tracking</param>
    /// <returns>An IQueryable for the entity type</returns>
    IQueryable<TEntity> GetQueryable(bool disableTracking = true);
}