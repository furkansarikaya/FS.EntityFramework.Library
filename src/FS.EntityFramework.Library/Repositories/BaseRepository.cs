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
    /// <param name="includes">Include expressions for related data</param>
    /// <param name="disableTracking">Whether to disable change tracking</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The entity if found, otherwise null</returns>
    public virtual async Task<TEntity?> GetByIdAsync(TKey id,
        List<Expression<Func<TEntity, object>>>? includes = null,
        bool disableTracking = false,
        CancellationToken cancellationToken = default)
    {
        var query = GetQueryable(disableTracking);
    
        if (includes != null && includes.Count != 0)
        {
            query = query.ApplyInclude(includes);
        }
    
        return await query.FirstOrDefaultAsync(e => e.Id.Equals(id), cancellationToken);
    }

    /// <summary>
    /// Gets all entities from the repository
    /// </summary>
    /// <param name="includes">Include expressions for related data</param>
    /// <param name="disableTracking">Whether to disable change tracking</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A read-only list of all entities</returns>
    public virtual async Task<IReadOnlyList<TEntity>> GetAllAsync(List<Expression<Func<TEntity, object>>>? includes = null,
        bool disableTracking = true,
        CancellationToken cancellationToken = default)
    {
        var query = GetQueryable(disableTracking);

        if (includes != null && includes.Count != 0)
        {
            query = query.ApplyInclude(includes);
        }

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
    /// <param name="includes">Include expressions for related data</param>
    /// <param name="disableTracking">Whether to disable change tracking</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The first matching entity or null</returns>
    public async Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate,
        List<Expression<Func<TEntity, object>>>? includes = null,
        bool disableTracking = true,
        CancellationToken cancellationToken = default)
    {
        var query = GetQueryable(disableTracking);

        if (includes != null && includes.Count != 0)
        {
            query = query.ApplyInclude(includes);
        }

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

    /// <inheritdoc />
    public virtual async Task<TEntity?> SingleOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        List<Expression<Func<TEntity, object>>>? includes = null,
        bool disableTracking = true,
        CancellationToken cancellationToken = default)
    {
        var query = GetQueryable(disableTracking);

        if (includes != null && includes.Count != 0)
        {
            query = query.ApplyInclude(includes);
        }

        return await query.SingleOrDefaultAsync(predicate, cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        var query = GetQueryable(true);

        return predicate == null
            ? await query.AnyAsync(cancellationToken)
            : await query.AnyAsync(predicate, cancellationToken);
    }

    #region Projection Methods Implementation

    /// <inheritdoc />
    public virtual async Task<TResult?> GetByIdAsync<TResult>(
        TKey id,
        Expression<Func<TEntity, TResult>> selector,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(e => e.Id.Equals(id))
            .Select(selector)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<IReadOnlyList<TResult>> GetAllAsync<TResult>(
        Expression<Func<TEntity, TResult>> selector,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Select(selector)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<TResult?> FirstOrDefaultAsync<TResult>(
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, TResult>> selector,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(predicate)
            .Select(selector)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<TResult> SingleOrDefaultAsync<TResult>(
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, TResult>> selector,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(predicate)
            .Select(selector)
            .SingleAsync(cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<IEnumerable<TResult>> FindAsync<TResult>(
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, TResult>> selector,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking().Where(predicate);

        if (orderBy != null)
            query = orderBy(query);

        return await query.Select(selector).ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<IPaginate<TResult>> GetPagedAsync<TResult>(
        Expression<Func<TEntity, TResult>> selector,
        int pageIndex,
        int pageSize,
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking();

        if (predicate != null)
            query = query.Where(predicate);

        if (orderBy != null)
            query = orderBy(query);

        return await query.Select(selector).ToPaginateAsync(pageIndex, pageSize, 0, cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<IPaginate<TResult>> GetPagedWithFilterAsync<TResult>(
        Expression<Func<TEntity, TResult>> selector,
        FilterModel filter,
        int pageIndex,
        int pageSize,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.AsNoTracking().ApplyFilter(filter);

        if (orderBy != null)
            query = orderBy(query);

        return await query.Select(selector).ToPaginateAsync(pageIndex, pageSize, 0, cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<IReadOnlyList<TResult>> GetAsync<TResult>(
        BaseSpecification<TEntity> spec,
        Expression<Func<TEntity, TResult>> selector,
        CancellationToken cancellationToken = default)
    {
        return await ApplySpecification(spec).Select(selector).ToListAsync(cancellationToken);
    }

    #endregion

    #region Cursor-Based Pagination Implementation

    /// <inheritdoc />
    public virtual async Task<ICursorPaginate<TEntity, TCursor>> GetCursorPagedAsync<TCursor>(
        int pageSize,
        TCursor? afterCursor,
        TCursor? beforeCursor,
        Expression<Func<TEntity, TCursor>> cursorSelector,
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        List<Expression<Func<TEntity, object>>>? includes = null,
        bool disableTracking = true,
        CancellationToken cancellationToken = default) where TCursor : IComparable<TCursor>
    {
        var query = GetQueryable(disableTracking);

        if (predicate != null)
            query = query.Where(predicate);

        if (includes != null && includes.Count != 0)
            query = query.ApplyInclude(includes);

        // Build cursor comparison
        var parameter = cursorSelector.Parameters[0];
        var cursorBody = cursorSelector.Body;

        if (afterCursor != null)
        {
            var afterComparison = Expression.GreaterThan(cursorBody, Expression.Constant(afterCursor, typeof(TCursor)));
            var afterLambda = Expression.Lambda<Func<TEntity, bool>>(afterComparison, parameter);
            query = query.Where(afterLambda);
        }
        else if (beforeCursor != null)
        {
            var beforeComparison = Expression.LessThan(cursorBody, Expression.Constant(beforeCursor, typeof(TCursor)));
            var beforeLambda = Expression.Lambda<Func<TEntity, bool>>(beforeComparison, parameter);
            query = query.Where(beforeLambda);
        }

        // Apply ordering or default to cursor ascending
        if (orderBy != null)
            query = orderBy(query);
        else
            query = query.OrderBy(cursorSelector);

        // Fetch one extra to determine if there are more
        var items = await query.Take(pageSize + 1).ToListAsync(cancellationToken);
        var hasNext = items.Count > pageSize;

        if (hasNext)
            items = items.Take(pageSize).ToList();

        var compiledSelector = cursorSelector.Compile();

        return new CursorPaginate<TEntity, TCursor>
        {
            Items = items,
            Size = pageSize,
            HasNext = hasNext,
            HasPrevious = afterCursor != null,
            FirstCursor = items.Count > 0 ? compiledSelector(items[0]) : default,
            LastCursor = items.Count > 0 ? compiledSelector(items[^1]) : default
        };
    }

    /// <inheritdoc />
    public virtual async Task<ICursorPaginate<TResult, TCursor>> GetCursorPagedAsync<TResult, TCursor>(
        Expression<Func<TEntity, TResult>> selector,
        int pageSize,
        TCursor? afterCursor,
        TCursor? beforeCursor,
        Expression<Func<TEntity, TCursor>> cursorSelector,
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        CancellationToken cancellationToken = default) where TCursor : IComparable<TCursor>
    {
        var query = DbSet.AsNoTracking();

        if (predicate != null)
            query = query.Where(predicate);

        var parameter = cursorSelector.Parameters[0];
        var cursorBody = cursorSelector.Body;

        if (afterCursor != null)
        {
            var afterComparison = Expression.GreaterThan(cursorBody, Expression.Constant(afterCursor, typeof(TCursor)));
            var afterLambda = Expression.Lambda<Func<TEntity, bool>>(afterComparison, parameter);
            query = query.Where(afterLambda);
        }
        else if (beforeCursor != null)
        {
            var beforeComparison = Expression.LessThan(cursorBody, Expression.Constant(beforeCursor, typeof(TCursor)));
            var beforeLambda = Expression.Lambda<Func<TEntity, bool>>(beforeComparison, parameter);
            query = query.Where(beforeLambda);
        }

        if (orderBy != null)
            query = orderBy(query);
        else
            query = query.OrderBy(cursorSelector);

        // We need to get cursors before projection, so we project with cursor
        var combinedSelector = BuildCombinedSelector(selector, cursorSelector);
        var items = await query.Take(pageSize + 1).Select(combinedSelector).ToListAsync(cancellationToken);

        var hasNext = items.Count > pageSize;
        if (hasNext)
            items = items.Take(pageSize).ToList();

        return new CursorPaginate<TResult, TCursor>
        {
            Items = items.Select(x => x.Result).ToList(),
            Size = pageSize,
            HasNext = hasNext,
            HasPrevious = afterCursor != null,
            FirstCursor = items.Count > 0 ? items[0].Cursor : default,
            LastCursor = items.Count > 0 ? items[^1].Cursor : default
        };
    }

    private static Expression<Func<TEntity, (TResult Result, TCursor Cursor)>> BuildCombinedSelector<TResult, TCursor>(
        Expression<Func<TEntity, TResult>> selector,
        Expression<Func<TEntity, TCursor>> cursorSelector)
    {
        var parameter = Expression.Parameter(typeof(TEntity), "e");

        var resultBody = new ParameterReplacer(parameter).Visit(selector.Body);
        var cursorBody = new ParameterReplacer(parameter).Visit(cursorSelector.Body);

        var tupleType = typeof(ValueTuple<TResult, TCursor>);
        var tupleConstructor = tupleType.GetConstructor([typeof(TResult), typeof(TCursor)])!;
        var newTuple = Expression.New(tupleConstructor, resultBody, cursorBody);

        return Expression.Lambda<Func<TEntity, (TResult Result, TCursor Cursor)>>(newTuple, parameter);
    }

    private class ParameterReplacer(ParameterExpression newParameter) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) => newParameter;
    }

    #endregion

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