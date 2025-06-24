using FS.EntityFramework.Library.Common;
using Microsoft.EntityFrameworkCore;

namespace FS.EntityFramework.Library.Extensions;

/// <summary>
/// Extension methods for configuring soft delete query filters
/// </summary>
public static class SoftDeleteQueryFilterExtensions
{
    /// <summary>
    /// Applies global query filter to exclude soft-deleted entities
    /// </summary>
    /// <param name="modelBuilder">Model builder</param>
    public static void ApplySoftDeleteQueryFilters(this ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;
            
            // Check if entity inherits from BaseAuditableEntity
            if (!IsAuditableEntity(clrType))
                continue;
            var method = typeof(SoftDeleteQueryFilterExtensions)
                .GetMethod(nameof(ApplyFilterToEntity), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                
            var genericMethod = method!.MakeGenericMethod(clrType);
            genericMethod.Invoke(null, [modelBuilder, entityType]);
        }
    }

    private static bool IsAuditableEntity(Type clrType)
    {
        var currentType = clrType;
        while (currentType != null)
        {
            if (currentType.IsGenericType && 
                currentType.GetGenericTypeDefinition() == typeof(BaseAuditableEntity<>))
            {
                return true;
            }
            currentType = currentType.BaseType;
        }
        return false;
    }

    private static void ApplyFilterToEntity<TEntity>(ModelBuilder modelBuilder, Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType entityType)
        where TEntity : class
    {
        var isDeletedProperty = entityType.FindProperty("IsDeleted");
        if (isDeletedProperty != null)
        {
            // Apply global query filter: WHERE IsDeleted = false
            modelBuilder.Entity<TEntity>().HasQueryFilter(e => !EF.Property<bool>(e, "IsDeleted"));
        }
    }

    /// <summary>
    /// Include soft-deleted entities in query (call this on specific queries when needed)
    /// </summary>
    public static IQueryable<TEntity> IncludeDeleted<TEntity>(this IQueryable<TEntity> query)
        where TEntity : class
    {
        return query.IgnoreQueryFilters();
    }

    /// <summary>
    /// Get only soft-deleted entities
    /// </summary>
    public static IQueryable<TEntity> OnlyDeleted<TEntity>(this IQueryable<TEntity> query)
        where TEntity : class
    {
        return query.IgnoreQueryFilters().Where(e => EF.Property<bool>(e, "IsDeleted"));
    }
}