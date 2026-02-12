using System.Diagnostics;
using FS.EntityFramework.Library.Common;
using FS.EntityFramework.Library.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace FS.EntityFramework.Library.Events;

/// <summary>
/// Default implementation of domain event dispatcher using dependency injection
/// </summary>
public class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly FSEntityFrameworkMetrics? _metrics;

    /// <summary>
    /// Initializes a new instance of the DomainEventDispatcher class
    /// </summary>
    /// <param name="serviceProvider">Service provider for resolving handlers</param>
    public DomainEventDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _metrics = serviceProvider.GetService<FSEntityFrameworkMetrics>();
    }

    /// <summary>
    /// Dispatches a single domain event to its handlers
    /// </summary>
    /// <param name="domainEvent">The domain event to dispatch</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var domainEventType = domainEvent.GetType();
        var eventTypeName = domainEventType.Name;
        var sw = _metrics != null ? Stopwatch.StartNew() : null;

        var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEventType);

        var handlers = _serviceProvider.GetServices(handlerType);
        var method = handlerType.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.Handle));

        var tasks = new List<Task>();
        foreach (var handler in handlers)
        {
            if (handler == null || method == null) continue;

            try
            {
                var result = method.Invoke(handler, [domainEvent, cancellationToken]);
                if (result is Task task)
                {
                    tasks.Add(WrapHandlerTask(task, eventTypeName, handler.GetType().Name));
                }
            }
            catch (Exception ex)
            {
                _metrics?.RecordEventHandlerError(eventTypeName, handler.GetType().Name);
                System.Diagnostics.Debug.WriteLine(
                    $"Error invoking handler {handler.GetType().Name} for event {eventTypeName}: {ex.Message}");
            }
        }

        await Task.WhenAll(tasks);

        _metrics?.RecordEventDispatched(eventTypeName);
        if (sw != null) _metrics?.RecordEventDispatchDuration(eventTypeName, sw.Elapsed.TotalMilliseconds);
    }

    /// <summary>
    /// Wraps a handler task to catch async exceptions individually,
    /// preventing a single faulted handler from crashing the entire dispatch.
    /// </summary>
    private async Task WrapHandlerTask(Task handlerTask, string eventTypeName, string handlerTypeName)
    {
        try
        {
            await handlerTask;
        }
        catch (Exception ex)
        {
            _metrics?.RecordEventHandlerError(eventTypeName, handlerTypeName);
            System.Diagnostics.Debug.WriteLine(
                $"Async error in handler {handlerTypeName} for event {eventTypeName}: {ex.Message}");
        }
    }

    /// <summary>
    /// Dispatches multiple domain events to their handlers
    /// </summary>
    /// <param name="domainEvents">The domain events to dispatch</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        // Dispatch events sequentially to preserve ordering (e.g., OrderCreated before OrderItemAdded)
        // Handlers within each event are still dispatched in parallel
        foreach (var domainEvent in domainEvents)
        {
            await DispatchAsync(domainEvent, cancellationToken);
        }
    }
}