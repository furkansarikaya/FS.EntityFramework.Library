using System.Linq.Expressions;
using FS.EntityFramework.Library.Common;
using FS.EntityFramework.Library.Extensions;
using FS.EntityFramework.Library.Interfaces;
using FS.EntityFramework.Library.Models;
using FS.EntityFramework.Library.Paging;
using FS.EntityFramework.Library.Specifications;
using Microsoft.EntityFrameworkCore;

namespace FS.EntityFramework.Library.Repositories;

public class BaseRepository<TEntity, TKey>(DbContext context) : IRepository<TEntity, TKey>
    where TEntity : BaseEntity<TKey>
    where TKey : IEquatable<TKey>
{
    protected readonly DbContext Context = context;
    protected readonly DbSet<TEntity> DbSet = context.Set<TEntity>();

    public virtual async Task<TEntity?> GetByIdAsync(TKey id, bool enableTracking = false, CancellationToken cancellationToken = default) =>
        enableTracking
            ? await DbSet.AsNoTracking().FirstOrDefaultAsync(e => e.Id.Equals(id), cancellationToken)
            : await DbSet.FindAsync([id], cancellationToken);

    public virtual async Task<IReadOnlyList<TEntity>> GetAllAsync(bool disableTracking = true, CancellationToken cancellationToken = default)
    {
        var query = GetQueryable();
        
        if (disableTracking) 
            query = query.AsNoTracking();
        return await query.ToListAsync(cancellationToken);
    }

    public virtual async Task<TEntity> AddAsync(TEntity entity, bool saveChanges = false, CancellationToken cancellationToken = default)
    {
        await DbSet.AddAsync(entity, cancellationToken);
        if (saveChanges)
            await SaveChangesAsync(cancellationToken);
        return entity;
    }

    public virtual async Task UpdateAsync(TEntity entity, bool saveChanges = false, CancellationToken cancellationToken = default)
    {
        Context.Entry(entity).State = EntityState.Modified;
        if (saveChanges)
            await SaveChangesAsync(cancellationToken);
    }

    public virtual async Task DeleteAsync(TEntity entity, bool saveChanges = false, bool isSoftDelete = true, CancellationToken cancellationToken = default)
    {
        var hasIsDeleted = typeof(TEntity).GetProperty("IsDeleted") != null;

        if (hasIsDeleted && isSoftDelete)
        {
            var isDeletedProperty = typeof(TEntity).GetProperty("IsDeleted");
            isDeletedProperty!.SetValue(entity, true);
            Context.Entry(entity).State = EntityState.Modified;
        }
        else
            DbSet.Remove(entity);

        if (saveChanges)
            await SaveChangesAsync(cancellationToken);
    }

    public virtual async Task DeleteAsync(TKey id, bool saveChanges = false, bool isSoftDelete = true, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken: cancellationToken);
        if (entity != null)
            await DeleteAsync(entity, saveChanges, isSoftDelete, cancellationToken);
    }

    public async Task BulkInsertAsync(IEnumerable<TEntity> entities, bool saveChanges = false, CancellationToken cancellationToken = default)
    {
        await DbSet.AddRangeAsync(entities, cancellationToken);
        if (saveChanges)
            await SaveChangesAsync(cancellationToken);
    }

    public async Task BulkUpdate(IEnumerable<TEntity> entities, bool saveChanges = false, CancellationToken cancellationToken = default)
    {
        DbSet.UpdateRange(entities);
        if (saveChanges)
            await SaveChangesAsync(cancellationToken);
    }

    public async Task BulkDeleteAsync(Expression<Func<TEntity, bool>> predicate, bool saveChanges = false, bool isSoftDelete = true, CancellationToken cancellationToken = default)
    {
        var hasIsDeleted = typeof(TEntity).GetProperty("IsDeleted") != null;

        if (hasIsDeleted && isSoftDelete)
        {
            var entities = await DbSet.Where(predicate).ToListAsync(cancellationToken);
            foreach (var entity in entities)
            {
                var isDeletedProperty = typeof(TEntity).GetProperty("IsDeleted");
                isDeletedProperty!.SetValue(entity, true);
                Context.Entry(entity).State = EntityState.Modified;
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

    public virtual async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => await Context.SaveChangesAsync(cancellationToken);

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

    public virtual async Task<IReadOnlyList<TEntity>> GetAsync(BaseSpecification<TEntity> spec, CancellationToken cancellationToken = default) => await ApplySpecification(spec).ToListAsync(cancellationToken);

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

    public async Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, bool disableTracking = true, CancellationToken cancellationToken = default)
    {
        var query = GetQueryable(disableTracking);
        return await query.FirstOrDefaultAsync(predicate, cancellationToken);
    }

    public async Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null, List<Expression<Func<TEntity, object>>>? includes = null, bool disableTracking = true, CancellationToken cancellationToken = default)
    {
        var query = GetQueryable(disableTracking);
        
        query = query.ApplySpecification(predicate)
                     .ApplyInclude(includes)
                     .ApplyOrder(orderBy);
                     
        return await query.ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var query = GetQueryable();
        return await query.AnyAsync(predicate, cancellationToken);
    }

    public virtual async Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default)
    {
        var query = GetQueryable();
        
        if (predicate != null)
            query = query.Where(predicate);
            
        return await query.CountAsync(cancellationToken);
    }

    public virtual IQueryable<TEntity> GetQueryable(bool disableTracking = true) => disableTracking ? DbSet.AsNoTracking() : DbSet;
    
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