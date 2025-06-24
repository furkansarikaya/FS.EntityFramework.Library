using FS.EntityFramework.Library.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FS.EntityFramework.Library.Interceptors;

/// <summary>
/// Interceptor that automatically sets audit properties on entities
/// </summary>
public class AuditInterceptor : SaveChangesInterceptor
{
    private readonly Func<string?> _getCurrentUser;
    private readonly Func<DateTime> _getCurrentTime;

    /// <summary>
    /// Creates audit interceptor with user provider function
    /// </summary>
    /// <param name="getCurrentUser">Function to get current user identifier</param>
    /// <param name="getCurrentTime">Function to get current time (optional, defaults to UTC now)</param>
    public AuditInterceptor(Func<string?> getCurrentUser, Func<DateTime>? getCurrentTime = null)
    {
        _getCurrentUser = getCurrentUser;
        _getCurrentTime = getCurrentTime ?? (() => DateTime.UtcNow);
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        UpdateAuditProperties(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        UpdateAuditProperties(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateAuditProperties(DbContext? context)
    {
        if (context == null) return;

        var entries = context.ChangeTracker.Entries()
            .Where(e => IsAuditableEntity(e.Entity) && 
                       (e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted))
            .ToList();

        var currentUser = _getCurrentUser?.Invoke();
        var now = _getCurrentTime!.Invoke();

        foreach (var entry in entries)
        {
            SetAuditProperties(entry, currentUser, now);
        }
    }

    private static bool IsAuditableEntity(object entity)
    {
        // Check if entity implements BaseAuditableEntity with any key type
        var entityType = entity.GetType();
        while (entityType != null)
        {
            if (entityType.IsGenericType && 
                entityType.GetGenericTypeDefinition() == typeof(BaseAuditableEntity<>))
            {
                return true;
            }
            entityType = entityType.BaseType;
        }
        return false;
    }

    private static void SetAuditProperties(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry, string? currentUser, DateTime now)
    {
        var entity = entry.Entity;
        var entityType = entity.GetType();

        switch (entry.State)
        {
            case EntityState.Added:
                SetProperty(entity, entityType, "CreatedAt", now);
                SetProperty(entity, entityType, "CreatedBy", currentUser);
                break;

            case EntityState.Modified:
                if (entry.Property("IsDeleted").CurrentValue is true &&
                    entry.Property("IsDeleted").OriginalValue is false)
                {
                    SetProperty(entity, entityType, "DeletedAt", now);
                    SetProperty(entity, entityType, "DeletedBy", currentUser);
                }
                else
                {
                    SetProperty(entity, entityType, "UpdatedAt", now);
                    SetProperty(entity, entityType, "UpdatedBy", currentUser);
                }
                break;
            case EntityState.Detached:
            case EntityState.Unchanged:
            case EntityState.Deleted:
            default:
                break;
        }
    }

    private static void SetProperty(object entity, Type entityType, string propertyName, object? value)
    {
        var property = entityType.GetProperty(propertyName);
        if (property != null && property.CanWrite)
        {
            property.SetValue(entity, value);
        }
    }
}