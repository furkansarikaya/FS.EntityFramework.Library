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

    /// <summary>
    /// Updates audit properties on entities in the given DbContext based on their state.
    /// </summary>
    /// <param name="context">The DbContext to process.</param>
    private void UpdateAuditProperties(DbContext? context)
    {
        if (context == null) return;

        var currentUser = _getCurrentUser?.Invoke();
        var now = _getCurrentTime!.Invoke();

        SetCreationAuditProperties(context, now, currentUser);
        SetModificationAuditProperties(context, now, currentUser);
        SetSoftDeleteAuditProperties(context, now, currentUser);
    }

    /// <summary>
    /// Sets creation audit properties for newly added entities.
    /// </summary>
    /// <param name="context">The DbContext to process.</param>
    /// <param name="now">The current date and time.</param>
    /// <param name="currentUser">The current user identifier.</param>
    private static void SetCreationAuditProperties(DbContext context, DateTime now, string? currentUser)
    {
        foreach (var entry in context.ChangeTracker.Entries<ICreationAuditableEntity>().Where(e => e.State == EntityState.Added))
        {
            SetProperty(entry.Entity, entry.Entity.GetType(), "CreatedAt", now);
            SetProperty(entry.Entity, entry.Entity.GetType(), "CreatedBy", currentUser);
        }
    }

    /// <summary>
    /// Sets modification audit properties for updated entities.
    /// </summary>
    /// <param name="context">The DbContext to process.</param>
    /// <param name="now">The current date and time.</param>
    /// <param name="currentUser">The current user identifier.</param>
    private static void SetModificationAuditProperties(DbContext context, DateTime now, string? currentUser)
    {
        foreach (var entry in context.ChangeTracker.Entries<IModificationAuditableEntity>().Where(e => e.State == EntityState.Modified))
        {
            SetProperty(entry.Entity, entry.Entity.GetType(), "UpdatedAt", now);
            SetProperty(entry.Entity, entry.Entity.GetType(), "UpdatedBy", currentUser);
        }
    }

    /// <summary>
    /// Sets soft delete audit properties for deleted entities.
    /// </summary>
    /// <param name="context">The DbContext to process.</param>
    /// <param name="now">The current date and time.</param>
    /// <param name="currentUser">The current user identifier.</param>
    private static void SetSoftDeleteAuditProperties(DbContext context, DateTime now, string? currentUser)
    {
        foreach (var entry in context.ChangeTracker.Entries<ISoftDelete>().Where(e => e.State == EntityState.Deleted))
        {
            entry.State = EntityState.Modified;
            SetProperty(entry.Entity, entry.Entity.GetType(), "IsDeleted", true);
            SetProperty(entry.Entity, entry.Entity.GetType(), "DeletedAt", now);
            SetProperty(entry.Entity, entry.Entity.GetType(), "DeletedBy", currentUser);
        }
    }

    /// <summary>
    /// Sets audit properties on entity
    /// </summary>
    /// <param name="entity">The entity</param>
    /// <param name="entityType">The entity type</param>
    /// <param name="propertyName">The property name</param>
    /// <param name="value">The value</param>
    private static void SetProperty(object entity, Type entityType, string propertyName, object? value)
    {
        var property = entityType.GetProperty(propertyName);
        if (property != null && property.CanWrite)
        {
            property.SetValue(entity, value);
        }
    }
}