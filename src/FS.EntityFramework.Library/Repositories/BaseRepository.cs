using System.Linq.Expressions;
using FS.EntityFramework.Library.Common;
using FS.EntityFramework.Library.Extensions;
using FS.EntityFramework.Library.Interfaces;
using FS.EntityFramework.Library.Models;
using FS.EntityFramework.Library.Paging;
using FS.EntityFramework.Library.Specifications;
using Microsoft.EntityFrameworkCore;

namespace FS.EntityFramework.Library.Repositories;

/// <summary>
/// Base repository implementation providing common CRUD operations and advanced querying capabilities
/// for entities that inherit from BaseEntity
/// </summary>
/// <typeparam name="TEntity">The entity type that inherits from BaseEntity</typeparam>
/// <typeparam name="TKey">The type of the entity's primary key</typeparam>
public class BaseRepository<TEntity, TKey>(DbContext context) : IRepository<TEntity, TKey>
    where TEntity : BaseEntity<TKey>
    where TKey : IEquatable<TKey>
{
    /// <summary>
    /// The Entity Framework context instance
    /// </summary>
    protected readonly DbContext Context = context;

    /// <summary>
    /// The DbSet for the entity type
    /// </summary>
    protected readonly DbSet<TEntity> DbSet = context.Set<TEntity>();

    /// <summary>
    /// Gets an entity by its primary key
    /// </summary>
    /// <param name="id">The primary key value</param>
    /// <param name="disableTracking">Whether to disable change tracking</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The entity if found, otherwise null</returns>
    public virtual async Task<TEntity?> GetByIdAsync(TKey id, bool disableTracking = false, CancellationToken cancellationToken = default) =>
        disableTracking
            ? await DbSet.AsNoTracking().FirstOrDefaultAsync(e => e.Id.Equals(id), cancellationToken)
            : await DbSet.FindAsync([id], cancellationToken);

    /// <summary>
    /// Gets all entities from the repository
    /// </summary>
    /// <param name="disableTracking">Whether to disable change tracking</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A read-only list of all entities</returns>
    public virtual async Task<IReadOnlyList<TEntity>> GetAllAsync(bool disableTracking = true, CancellationToken cancellationToken = default)
    {
        var query = GetQueryable();

        if (disableTracking)
            query = query.AsNoTracking();
        return await query.ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Adds a new entity to the repository
    /// </summary>
    /// <param name="entity">The entity to add</param>
    /// <param name="saveChanges">Whether to immediately save changes to the database</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The added entity</returns>
    public virtual async Task<TEntity> AddAsync(TEntity entity, bool saveChanges = false, CancellationToken cancellationToken = default)
    {
        await DbSet.AddAsync(entity, cancellationToken);
        if (saveChanges)
            await SaveChangesAsync(cancellationToken);
        return entity;
    }

    /// <summary>
    /// Updates an existing entity in the repository
    /// </summary>
    /// <param name="entity">The entity to update</param>
    /// <param name="saveChanges">Whether to immediately save changes to the database</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public virtual async Task UpdateAsync(TEntity entity, bool saveChanges = false, CancellationToken cancellationToken = default)
    {
        Context.Entry(entity).State = EntityState.Modified;
        if (saveChanges)
            await SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Deletes an entity from the repository (supports soft delete)
    /// </summary>
    /// <param name="entity">The entity to delete</param>
    /// <param name="saveChanges">Whether to immediately save changes to the database</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public virtual async Task DeleteAsync(TEntity entity, bool saveChanges = false, CancellationToken cancellationToken = default)
    {
        Context.Entry(entity).State = EntityState.Deleted;

        if (saveChanges)
            await SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Deletes an entity by its primary key (supports soft delete)
    /// </summary>
    /// <param name="id">The primary key of the entity to delete</param>
    /// <param name="saveChanges">Whether to immediately save changes to the database</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public virtual async Task DeleteAsync(TKey id, bool saveChanges = false, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken: cancellationToken);
        if (entity != null)
            await DeleteAsync(entity, saveChanges, cancellationToken);
    }

    /// <summary>
    /// Hard deletes an entity (permanently deletes it from the database)
    /// </summary>
    /// <param name="entity">The entity to delete</param>
    /// <param name="saveChanges">Whether to immediately save changes to the database</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task HardDeleteAsync(TEntity entity, bool saveChanges = false, CancellationToken cancellationToken = default)
    {
        context.EnableBypassSoftDelete();
        Context.Remove(entity);
        if (saveChanges)
            await SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Hard deletes an entity by its primary key (permanently deletes it from the database)
    /// </summary>
    /// <param name="id">The primary key of the entity to delete</param>
    /// <param name="saveChanges">Whether to immediately save changes to the database</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task HardDeleteAsync(TKey id, bool saveChanges = false, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken: cancellationToken);
        if (entity != null)
            await HardDeleteAsync(entity, saveChanges, cancellationToken);
    }

    /// <summary>
    /// Restores a soft deleted entity (only if entity implements ISoftDelete)
    /// </summary>
    public virtual async Task RestoreAsync(TEntity entity, bool saveChanges = false, CancellationToken cancellationToken = default)
    {
        if (entity is not ISoftDelete softDeleteEntity)
        {
            throw new InvalidOperationException($"Entity {typeof(TEntity).Name} does not implement ISoftDelete interface");
        }

        softDeleteEntity.IsDeleted = false;
        softDeleteEntity.DeletedAt = null;
        softDeleteEntity.DeletedBy = null;
        Context.Entry(entity).State = EntityState.Modified;

        if (saveChanges)
            await SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Restores a soft deleted entity by its primary key (only if entity implements ISoftDelete)
    /// </summary>
    public virtual async Task RestoreAsync(TKey id, bool saveChanges = false, CancellationToken cancellationToken = default)
    {
        // We need to ignore query filters to find soft-deleted entities
        var entity = await DbSet.IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.Id.Equals(id), cancellationToken);

        if (entity != null)
            await RestoreAsync(entity, saveChanges, cancellationToken);
    }

    /// <summary>
    /// Adds multiple entities to the repository in a single operation
    /// </summary>
    /// <param name="entities">The entities to add</param>
    /// <param name="saveChanges">Whether to immediately save changes to the database</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task BulkInsertAsync(IEnumerable<TEntity> entities, bool saveChanges = false, CancellationToken cancellationToken = default)
    {
        await DbSet.AddRangeAsync(entities, cancellationToken);
        if (saveChanges)
            await SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Updates multiple entities in the repository in a single operation
    /// </summary>
    /// <param name="entities">The entities to update</param>
    /// <param name="saveChanges">Whether to immediately save changes to the database</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task BulkUpdate(IEnumerable<TEntity> entities, bool saveChanges = false, CancellationToken cancellationToken = default)
    {
        DbSet.UpdateRange(entities);
        if (saveChanges)
            await SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Deletes multiple entities matching the predicate in a single operation (supports soft delete)
    /// </summary>
    /// <param name="predicate">The predicate to filter entities for deletion</param>
    /// <param name="saveChanges">Whether to immediately save changes to the database</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task BulkDeleteAsync(Expression<Func<TEntity, bool>> predicate, bool saveChanges = false, CancellationToken cancellationToken = default)
    {
        var hasIsDeleted = typeof(TEntity).GetProperty("IsDeleted") != null;
        if (hasIsDeleted)
        {
            var entities = await DbSet.Where(predicate).ToListAsync(cancellationToken);
            foreach (var entity in entities)
            {
                Context.Entry(entity).State = EntityState.Deleted;
            }
        }
        else
        {
            var entities = await DbSet.Where(predicate).ToListAsync(cancellationToken);
            DbSet.RemoveRange(entities);
        }

        if (saveChanges)
            await SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Saves all pending changes to the database
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The number of state entries written to the database</returns>
    public virtual async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var result = await Context.SaveChangesAsync(cancellationToken);
        if(Context.IsBypassSoftDeleteEnabled())
            Context.DisableBypassSoftDelete();
        return result;
    }

    /// <summary>
    /// Gets entities with string-based include for related data
    /// </summary>
    /// <param name="predicate">Filter predicate</param>
    /// <param name="orderBy">Ordering function</param>
    /// <param name="includeString">Include string for related data</param>
    /// <param name="disableTracking">Whether to disable change tracking</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A read-only list of entities</returns>
    public virtual async Task<IReadOnlyList<TEntity>> GetWithIncludeStringAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        string? includeString = null,
        bool disableTracking = true,
        CancellationToken cancellationToken = default)
    {
        var query = GetQueryable();

        if (disableTracking)
            query = query.AsNoTracking();

        query = query.ApplySpecification(predicate)
            .ApplyInclude(includeString)
            .ApplyOrder(orderBy);

        return await query.ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets entities with expression-based includes for related data
    /// </summary>
    /// <param name="predicate">Filter predicate</param>
    /// <param name="orderBy">Ordering function</param>
    /// <param name="includes">Include expressions for related data</param>
    /// <param name="disableTracking">Whether to disable change tracking</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A read-only list of entities</returns>
    public virtual async Task<IReadOnlyList<TEntity>> GetWithIncludesAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        List<Expression<Func<TEntity, object>>>? includes = null,
        bool disableTracking = true,
        CancellationToken cancellationToken = default)
    {
        var query = GetQueryable();

        if (disableTracking)
            query = query.AsNoTracking();

        query = query.ApplySpecification(predicate)
            .ApplyInclude(includes)
            .ApplyOrder(orderBy);

        return await query.ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets entities using the specification pattern
    /// </summary>
    /// <param name="spec">The specification containing query criteria</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A read-only list of entities matching the specification</returns>
    public virtual async Task<IReadOnlyList<TEntity>> GetAsync(BaseSpecification<TEntity> spec, CancellationToken cancellationToken = default) => await ApplySpecification(spec).ToListAsync(cancellationToken);

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
    public virtual async Task<IPaginate<TEntity>> GetPagedAsync(
        int pageIndex,
        int pageSize,
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        List<Expression<Func<TEntity, object>>>? includes = null,
        bool disableTracking = true,
        CancellationToken cancellationToken = default)
    {
        var query = GetQueryable();

        if (disableTracking)
            query = query.AsNoTracking();

        query = query.ApplySpecification(predicate)
            .ApplyInclude(includes)
            .ApplyOrder(orderBy);

        return await query.ToPaginateAsync(pageIndex, pageSize, 0, cancellationToken);
    }

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
    public virtual async Task<IPaginate<TEntity>> GetPagedWithFilterAsync(
        FilterModel filter,
        int pageIndex,
        int pageSize,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        List<Expression<Func<TEntity, object>>>? includes = null,
        bool disableTracking = true,
        CancellationToken cancellationToken = default)
    {
        var query = GetQueryable();

        if (disableTracking)
            query = query.AsNoTracking();

        query = query.ApplyFilter(filter)
            .ApplyInclude(includes)
            .ApplyOrder(orderBy);

        return await query.ToPaginateAsync(pageIndex, pageSize, 0, cancellationToken);
    }

    /// <summary>
    /// Gets the first entity matching the predicate or null if not found
    /// </summary>
    /// <param name="predicate">The predicate to match</param>
    /// <param name="disableTracking">Whether to disable change tracking</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The first matching entity or null</returns>
    public async Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, bool disableTracking = true, CancellationToken cancellationToken = default)
    {
        var query = GetQueryable(disableTracking);
        return await query.FirstOrDefaultAsync(predicate, cancellationToken);
    }

    /// <summary>
    /// Finds entities matching the predicate with optional ordering and includes
    /// </summary>
    /// <param name="predicate">The predicate to match</param>
    /// <param name="orderBy">Ordering function</param>
    /// <param name="includes">Include expressions for related data</param>
    /// <param name="disableTracking">Whether to disable change tracking</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>An enumerable of matching entities</returns>
    public async Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null, List<Expression<Func<TEntity, object>>>? includes = null, bool disableTracking = true, CancellationToken cancellationToken = default)
    {
        var query = GetQueryable(disableTracking);

        query = query.ApplySpecification(predicate)
            .ApplyInclude(includes)
            .ApplyOrder(orderBy);

        return await query.ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Checks if any entity matches the predicate
    /// </summary>
    /// <param name="predicate">The predicate to match</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if any entity matches the predicate; otherwise, false</returns>
    public async Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var query = GetQueryable();
        return await query.AnyAsync(predicate, cancellationToken);
    }

    /// <summary>
    /// Gets the count of entities matching the predicate
    /// </summary>
    /// <param name="predicate">The predicate to match (optional, counts all if null)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The count of matching entities</returns>
    public virtual async Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default)
    {
        var query = GetQueryable();

        if (predicate != null)
            query = query.Where(predicate);
        return await query.CountAsync(cancellationToken);
    }

    /// <summary>
    /// Gets the raw IQueryable for advanced LINQ operations
    /// </summary>
    /// <param name="disableTracking">Whether to disable change tracking</param>
    /// <returns>An IQueryable for the entity type</returns>
    public virtual IQueryable<TEntity> GetQueryable(bool disableTracking = true) => disableTracking ? DbSet.AsNoTracking() : DbSet;

    /// <summary>
    /// Applies a specification to the queryable and returns the configured query
    /// </summary>
    /// <param name="spec">The specification to apply</param>
    /// <returns>The configured query with specification applied</returns>
    private IQueryable<TEntity> ApplySpecification(BaseSpecification<TEntity> spec)
    {
        var query = GetQueryable();

        if (spec.Criteria != null)
            query = query.Where(spec.Criteria);

        query = spec.Includes.Aggregate(query, (current, include) => current.Include(include));

        query = spec.IncludeStrings.Aggregate(query, (current, include) => current.Include(include));

        if (spec.OrderBy != null)
            query = query.OrderBy(spec.OrderBy);

        if (spec.OrderByDescending != null)
            query = query.OrderByDescending(spec.OrderByDescending);

        if (spec.GroupBy != null)
            query = query.GroupBy(spec.GroupBy).SelectMany(x => x);

        if (spec.IsPagingEnabled)
            query = query.Skip(spec.Skip).Take(spec.Take);

        return query;
    }
}