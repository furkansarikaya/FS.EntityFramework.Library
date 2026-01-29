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
    /// <param name="includes">Include expressions for related data</param>
    /// <param name="disableTracking">Whether to disable change tracking</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The entity if found, otherwise null</returns>
    Task<TEntity?> GetByIdAsync(TKey id,
        List<Expression<Func<TEntity, object>>>? includes = null,
        bool disableTracking = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all entities from the repository
    /// </summary>
    /// <param name="includes">Include expressions for related data</param>
    /// <param name="disableTracking">Whether to disable change tracking</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A read-only list of all entities</returns>
    Task<IReadOnlyList<TEntity>> GetAllAsync(List<Expression<Func<TEntity, object>>>? includes = null,
        bool disableTracking = true,
        CancellationToken cancellationToken = default);
    
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
    /// Hard deletes an entity (permanently deletes it from the database)
    /// </summary>
    /// <param name="entity">The entity to delete</param>
    /// <param name="saveChanges">Whether to immediately save changes to the database</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task HardDeleteAsync(TEntity entity, bool saveChanges = false, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Hard deletes an entity by its primary key (permanently deletes it from the database)
    /// </summary>
    /// <param name="id">The primary key of the entity to delete</param>
    /// <param name="saveChanges">Whether to immediately save changes to the database</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task HardDeleteAsync(TKey id, bool saveChanges = false, CancellationToken cancellationToken = default);
    
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
    /// <param name="includes">Include expressions for related data</param>
    /// <param name="disableTracking">Whether to disable change tracking</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The first matching entity or null</returns>
    Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate,
        List<Expression<Func<TEntity, object>>>? includes = null,
        bool disableTracking = true,
        CancellationToken cancellationToken = default);

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

    /// <summary>
    /// Gets the single entity matching the predicate or null if not found.
    /// Throws an exception if more than one entity matches the predicate.
    /// Use this for queries where you expect zero or one result.
    /// </summary>
    /// <param name="predicate">A filter expression that should match at most one entity.</param>
    /// <param name="includes">Include expressions for related data.</param>
    /// <param name="disableTracking">Whether to disable change tracking.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The entity if found; otherwise null.</returns>
    /// <exception cref="InvalidOperationException">Thrown when more than one entity matches the predicate.</exception>
    Task<TEntity?> SingleOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        List<Expression<Func<TEntity, object>>>? includes = null,
        bool disableTracking = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if any entity matches the optional predicate.
    /// More readable alternative to ExistsAsync.
    /// </summary>
    /// <param name="predicate">Optional filter predicate. If null, checks if any entities exist.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>True if any entity matches; otherwise false.</returns>
    Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default);

    // Raw IQueryable (ileri düzey LINQ sorguları için)

    /// <summary>
    /// Gets the raw IQueryable for advanced LINQ operations
    /// </summary>
    /// <param name="disableTracking">Whether to disable change tracking</param>
    /// <returns>An IQueryable for the entity type</returns>
    IQueryable<TEntity> GetQueryable(bool disableTracking = true);

    #region Projection Methods

    /// <summary>
    /// Gets an entity by its primary key and projects it to the specified result type.
    /// This method is ideal for retrieving a single entity and transforming it to a DTO or view model
    /// without loading the entire entity graph into memory.
    /// </summary>
    /// <typeparam name="TResult">The type to project the entity to. Can be a DTO, anonymous type, or any class.</typeparam>
    /// <param name="id">The primary key value of the entity to retrieve.</param>
    /// <param name="selector">
    /// A projection expression that defines how to transform the entity to the result type.
    /// Example: x => new ProductDto { Id = x.Id, Name = x.Name, CategoryName = x.Category.Name }
    /// </param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>
    /// The projected result if the entity is found; otherwise null.
    /// The query is executed with tracking disabled for optimal read performance.
    /// </returns>
    Task<TResult?> GetByIdAsync<TResult>(
        TKey id,
        Expression<Func<TEntity, TResult>> selector,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all entities and projects them to the specified result type.
    /// This method is optimized for read-only scenarios where you need to retrieve
    /// all records transformed to DTOs or view models.
    /// </summary>
    /// <typeparam name="TResult">The type to project each entity to.</typeparam>
    /// <param name="selector">
    /// A projection expression that defines how to transform each entity.
    /// The expression is translated to SQL SELECT, so only the specified columns are fetched.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A read-only list of projected results. Returns an empty list if no entities exist.</returns>
    Task<IReadOnlyList<TResult>> GetAllAsync<TResult>(
        Expression<Func<TEntity, TResult>> selector,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the first entity matching the predicate and projects it to the specified result type.
    /// Useful for finding a single entity by a business condition and transforming it in one query.
    /// </summary>
    /// <typeparam name="TResult">The type to project the entity to.</typeparam>
    /// <param name="predicate">A filter expression to find the entity.</param>
    /// <param name="selector">A projection expression defining the transformation.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The projected result if found; otherwise null.</returns>
    Task<TResult?> FirstOrDefaultAsync<TResult>(
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, TResult>> selector,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the single entity matching the predicate and projects it to the specified result type.
    /// Throws an exception if zero or more than one entity matches the predicate.
    /// Use this method when you expect exactly one result for data integrity.
    /// </summary>
    /// <typeparam name="TResult">The type to project the entity to.</typeparam>
    /// <param name="predicate">A filter expression that should match exactly one entity.</param>
    /// <param name="selector">A projection expression defining the transformation.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The projected result.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no entity matches the predicate or when more than one entity matches.
    /// </exception>
    Task<TResult> SingleOrDefaultAsync<TResult>(
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, TResult>> selector,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds all entities matching the predicate, applies optional ordering, and projects to result type.
    /// Provides full control over filtering, ordering, and projection in a single optimized query.
    /// </summary>
    /// <typeparam name="TResult">The type to project each entity to.</typeparam>
    /// <param name="predicate">A filter expression to match entities.</param>
    /// <param name="selector">A projection expression defining the transformation.</param>
    /// <param name="orderBy">Optional ordering function applied before projection.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A collection of projected results matching the predicate.</returns>
    Task<IEnumerable<TResult>> FindAsync<TResult>(
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, TResult>> selector,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a paginated list of entities and projects each to the specified result type.
    /// Essential for building efficient list views with pagination where only specific fields are needed.
    /// </summary>
    /// <typeparam name="TResult">The type to project each entity to.</typeparam>
    /// <param name="selector">A projection expression defining the transformation.</param>
    /// <param name="pageIndex">The zero-based page index.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="predicate">Optional filter to apply before pagination.</param>
    /// <param name="orderBy">Optional ordering function. Recommended for consistent pagination.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A paginated result containing projected items and pagination metadata.</returns>
    Task<IPaginate<TResult>> GetPagedAsync<TResult>(
        Expression<Func<TEntity, TResult>> selector,
        int pageIndex,
        int pageSize,
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a paginated list with dynamic filtering and projects to the specified result type.
    /// Combines the power of dynamic filtering with projection for advanced search interfaces.
    /// </summary>
    /// <typeparam name="TResult">The type to project each entity to.</typeparam>
    /// <param name="selector">A projection expression defining the transformation.</param>
    /// <param name="filter">Dynamic filter model containing search term and field-specific filters.</param>
    /// <param name="pageIndex">The zero-based page index.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="orderBy">Optional ordering function.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A paginated result of projected items matching the filter criteria.</returns>
    Task<IPaginate<TResult>> GetPagedWithFilterAsync<TResult>(
        Expression<Func<TEntity, TResult>> selector,
        FilterModel filter,
        int pageIndex,
        int pageSize,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets entities using the specification pattern and projects to the specified result type.
    /// Allows combining reusable query specifications with flexible projection.
    /// </summary>
    /// <typeparam name="TResult">The type to project each entity to.</typeparam>
    /// <param name="spec">The specification containing query criteria, includes, and ordering.</param>
    /// <param name="selector">A projection expression defining the transformation.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A read-only list of projected results matching the specification.</returns>
    Task<IReadOnlyList<TResult>> GetAsync<TResult>(
        BaseSpecification<TEntity> spec,
        Expression<Func<TEntity, TResult>> selector,
        CancellationToken cancellationToken = default);

    #endregion

    #region Cursor-Based Pagination

    /// <summary>
    /// Gets a cursor-based paginated list of entities.
    /// Cursor pagination is more efficient than offset pagination for large datasets.
    /// </summary>
    /// <typeparam name="TCursor">The type of the cursor (usually the entity's ID type).</typeparam>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="afterCursor">Return items after this cursor value (for forward pagination).</param>
    /// <param name="beforeCursor">Return items before this cursor value (for backward pagination).</param>
    /// <param name="cursorSelector">Expression to select the cursor value from the entity.</param>
    /// <param name="predicate">Optional filter predicate.</param>
    /// <param name="orderBy">Optional ordering function.</param>
    /// <param name="includes">Include expressions for related data.</param>
    /// <param name="disableTracking">Whether to disable change tracking.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A cursor-paginated result.</returns>
    Task<ICursorPaginate<TEntity, TCursor>> GetCursorPagedAsync<TCursor>(
        int pageSize,
        TCursor? afterCursor,
        TCursor? beforeCursor,
        Expression<Func<TEntity, TCursor>> cursorSelector,
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        List<Expression<Func<TEntity, object>>>? includes = null,
        bool disableTracking = true,
        CancellationToken cancellationToken = default) where TCursor : IComparable<TCursor>;

    /// <summary>
    /// Gets a cursor-based paginated list with projection.
    /// </summary>
    /// <typeparam name="TResult">The type to project each entity to.</typeparam>
    /// <typeparam name="TCursor">The type of the cursor.</typeparam>
    /// <param name="selector">A projection expression defining the transformation.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="afterCursor">Return items after this cursor value (for forward pagination).</param>
    /// <param name="beforeCursor">Return items before this cursor value (for backward pagination).</param>
    /// <param name="cursorSelector">Expression to select the cursor value from the entity.</param>
    /// <param name="predicate">Optional filter predicate.</param>
    /// <param name="orderBy">Optional ordering function.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A cursor-paginated result with projected items.</returns>
    Task<ICursorPaginate<TResult, TCursor>> GetCursorPagedAsync<TResult, TCursor>(
        Expression<Func<TEntity, TResult>> selector,
        int pageSize,
        TCursor? afterCursor,
        TCursor? beforeCursor,
        Expression<Func<TEntity, TCursor>> cursorSelector,
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        CancellationToken cancellationToken = default) where TCursor : IComparable<TCursor>;

    #endregion
}