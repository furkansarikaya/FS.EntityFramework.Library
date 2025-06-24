using System.Linq.Expressions;
using FS.EntityFramework.Library.Common;
using FS.EntityFramework.Library.Models;
using FS.EntityFramework.Library.Paging;
using FS.EntityFramework.Library.Specifications;

namespace FS.EntityFramework.Library.Interfaces;

public interface IRepository<TEntity, in TKey> 
    where TEntity : IEntity<TKey>
    where TKey : IEquatable<TKey>
{
    // Temel CRUD Operasyonları
    Task<TEntity?> GetByIdAsync(TKey id, bool disableTracking = false, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TEntity>> GetAllAsync(bool disableTracking = true, CancellationToken cancellationToken = default);
    Task<TEntity> AddAsync(TEntity entity, bool saveChanges = false, CancellationToken cancellationToken = default);
    Task UpdateAsync(TEntity entity, bool saveChanges = false, CancellationToken cancellationToken = default);
    Task DeleteAsync(TEntity entity, bool saveChanges = false, bool isSoftDelete = true, CancellationToken cancellationToken = default);
    Task DeleteAsync(TKey id, bool saveChanges = false, bool isSoftDelete = true, CancellationToken cancellationToken = default);
    
    // Bulk Operations
    Task BulkInsertAsync(IEnumerable<TEntity> entities, bool saveChanges = false, CancellationToken cancellationToken = default);
    Task BulkUpdate(IEnumerable<TEntity> entities, bool saveChanges = false, CancellationToken cancellationToken = default);
    Task BulkDeleteAsync(Expression<Func<TEntity, bool>> predicate, bool saveChanges = false, bool isSoftDelete = true, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    // Gelişmiş Sorgulama
    Task<IReadOnlyList<TEntity>> GetWithIncludeStringAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        string? includeString = null,
        bool disableTracking = true,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TEntity>> GetWithIncludesAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        List<Expression<Func<TEntity, object>>>? includes = null,
        bool disableTracking = true,
        CancellationToken cancellationToken = default);
    
    // Specification pattern kullanarak sorgulama
    Task<IReadOnlyList<TEntity>> GetAsync(BaseSpecification<TEntity> spec,
        CancellationToken cancellationToken = default);
    
    // Sayfalama için
    Task<IPaginate<TEntity>> GetPagedAsync(
        int pageIndex, 
        int pageSize,
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        List<Expression<Func<TEntity, object>>>? includes = null,
        bool disableTracking = true,
        CancellationToken cancellationToken = default);
    
    // Dinamik Filtreleme
    Task<IPaginate<TEntity>> GetPagedWithFilterAsync(
        FilterModel filter,
        int pageIndex,
        int pageSize,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        List<Expression<Func<TEntity, object>>>? includes = null,
        bool disableTracking = true,
        CancellationToken cancellationToken = default);
    
    // Advanced Query Operations
    Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, bool disableTracking = true, CancellationToken cancellationToken = default);

    Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        List<Expression<Func<TEntity, object>>>? includes = null,
        bool disableTracking = true,
        CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
    Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default);
    
    // Raw IQueryable (ileri düzey LINQ sorguları için)
    IQueryable<TEntity> GetQueryable(bool disableTracking = true);
}