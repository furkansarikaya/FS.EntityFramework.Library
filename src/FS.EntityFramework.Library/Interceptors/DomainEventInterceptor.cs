using FS.EntityFramework.Library.Common;
using FS.EntityFramework.Library.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FS.EntityFramework.Library.Interceptors;

/// <summary>
/// Interceptor that automatically dispatches domain events when saving changes
/// </summary>
public class DomainEventInterceptor : SaveChangesInterceptor
{
    private readonly IDomainEventDispatcher? _domainEventDispatcher;

    /// <summary>
    /// Initializes a new instance of the DomainEventInterceptor class
    /// </summary>
    /// <param name="domainEventDispatcher">Optional domain event dispatcher</param>
    public DomainEventInterceptor(IDomainEventDispatcher? domainEventDispatcher = null)
    {
        _domainEventDispatcher = domainEventDispatcher;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, 
        InterceptionResult<int> result, 
        CancellationToken cancellationToken = default)
    {
        await DispatchDomainEventsAsync(eventData.Context, cancellationToken);
        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, 
        InterceptionResult<int> result)
    {
        DispatchDomainEventsAsync(eventData.Context).GetAwaiter().GetResult();
        return base.SavingChanges(eventData, result);
    }

    private async Task DispatchDomainEventsAsync(DbContext? context, CancellationToken cancellationToken = default)
    {
        if (context == null || _domainEventDispatcher == null)
            return;

        var entitiesWithEvents = context.ChangeTracker.Entries<IHasDomainEvents>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

        var domainEvents = entitiesWithEvents
            .SelectMany(e => e.DomainEvents)
            .ToList();

        // Clear events before dispatching to avoid duplicate processing
        entitiesWithEvents.ForEach(e => e.ClearDomainEvents());

        if (domainEvents.Count > 0)
        {
            await _domainEventDispatcher.DispatchAsync(domainEvents, cancellationToken);
        }
    }
}