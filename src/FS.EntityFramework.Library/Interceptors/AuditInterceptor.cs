using FS.EntityFramework.Library.Common;
using FS.EntityFramework.Library.Extensions;
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

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        UpdateAuditProperties(eventData.Context);
        return await base.SavingChangesAsync(eventData, result, cancellationToken);
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
        var softDeleteEntries = context.ChangeTracker.Entries<ISoftDelete>()
            .Where(e => e.State == EntityState.Deleted)
            .ToList(); // ToList() ile lazy evaluation'ı önle

        if (context?.IsBypassSoftDeleteEnabled() == true)
        {
            context.DisableBypassSoftDelete();
            return;
        }
        
        foreach (var entry in softDeleteEntries)
        {
            entry.State = EntityState.Unchanged;

            // Sadece gerekli alanları değiştir
            var entity = entry.Entity;
            entity.IsDeleted = true;
            entity.DeletedAt = now;
            entity.DeletedBy = currentUser;
            
            // Sadece bu alanların modified olduğunu belirt
            entry.Property(e => e.IsDeleted).IsModified = true;
            entry.Property(e => e.DeletedAt).IsModified = true;
            entry.Property(e => e.DeletedBy).IsModified = true;
            
            // EF7+ support
            foreach (var complex in entry.ComplexProperties)
                complex.IsModified = false;

            // Fallback: for EF6/EF7 if ComplexProperties is empty
            foreach (var reference in entry.References)
                reference.IsModified = false;
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