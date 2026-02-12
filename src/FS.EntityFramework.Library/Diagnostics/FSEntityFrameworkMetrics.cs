using System.Diagnostics.Metrics;

namespace FS.EntityFramework.Library.Diagnostics;

/// <summary>
/// OpenTelemetry-compatible metrics for FS.EntityFramework.Library using System.Diagnostics.Metrics.
/// Metrics are opt-in and must be enabled via .WithMetrics() in the fluent configuration.
/// </summary>
public sealed class FSEntityFrameworkMetrics : IDisposable
{
    /// <summary>
    /// The meter name used for all FS.EntityFramework.Library metrics
    /// </summary>
    public const string MeterName = "FS.EntityFramework.Library";

    private readonly Meter _meter;

    // Repository metrics
    private readonly Counter<long> _repositoryOperations;
    private readonly Counter<long> _repositoryOperationErrors;
    private readonly Histogram<double> _repositoryOperationDuration;

    // UnitOfWork metrics
    private readonly Counter<long> _saveChanges;
    private readonly Counter<long> _transactions;
    private readonly Counter<long> _cacheHits;
    private readonly Counter<long> _cacheMisses;

    // Interceptor metrics
    private readonly Counter<long> _auditEntities;
    private readonly Counter<long> _idGeneration;

    // Domain Event metrics
    private readonly Counter<long> _eventsDispatched;
    private readonly Counter<long> _eventHandlerErrors;
    private readonly Histogram<double> _eventDispatchDuration;

    /// <summary>
    /// Initializes a new instance of the FSEntityFrameworkMetrics class
    /// </summary>
    public FSEntityFrameworkMetrics()
    {
        _meter = new Meter(MeterName, "10.0.3");

        _repositoryOperations = _meter.CreateCounter<long>(
            "repository.operations",
            description: "Total number of repository operations");

        _repositoryOperationErrors = _meter.CreateCounter<long>(
            "repository.operations.errors",
            description: "Total number of repository operation errors");

        _repositoryOperationDuration = _meter.CreateHistogram<double>(
            "repository.operation.duration",
            unit: "ms",
            description: "Duration of repository operations in milliseconds");

        _saveChanges = _meter.CreateCounter<long>(
            "unitofwork.savechanges",
            description: "Total number of SaveChanges calls");

        _transactions = _meter.CreateCounter<long>(
            "unitofwork.transactions",
            description: "Total number of transaction operations");

        _cacheHits = _meter.CreateCounter<long>(
            "unitofwork.cache.hits",
            description: "Total number of repository cache hits");

        _cacheMisses = _meter.CreateCounter<long>(
            "unitofwork.cache.misses",
            description: "Total number of repository cache misses");

        _auditEntities = _meter.CreateCounter<long>(
            "interceptor.audit.entities",
            description: "Total number of entities processed by audit interceptor");

        _idGeneration = _meter.CreateCounter<long>(
            "interceptor.idgeneration.generated",
            description: "Total number of IDs generated");

        _eventsDispatched = _meter.CreateCounter<long>(
            "events.dispatched",
            description: "Total number of domain events dispatched");

        _eventHandlerErrors = _meter.CreateCounter<long>(
            "events.handler.errors",
            description: "Total number of domain event handler errors");

        _eventDispatchDuration = _meter.CreateHistogram<double>(
            "events.dispatch.duration",
            unit: "ms",
            description: "Duration of domain event dispatch in milliseconds");
    }

    /// <summary>
    /// Records a repository operation
    /// </summary>
    /// <param name="operation">The operation type (add, update, delete, query, bulk_insert, bulk_delete)</param>
    public void RecordRepositoryOperation(string operation)
    {
        _repositoryOperations.Add(1, new KeyValuePair<string, object?>("operation", operation));
    }

    /// <summary>
    /// Records a repository operation error
    /// </summary>
    /// <param name="operation">The operation type</param>
    /// <param name="errorType">The error type name</param>
    public void RecordRepositoryError(string operation, string errorType)
    {
        _repositoryOperationErrors.Add(1,
            new KeyValuePair<string, object?>("operation", operation),
            new KeyValuePair<string, object?>("error_type", errorType));
    }

    /// <summary>
    /// Records the duration of a repository operation
    /// </summary>
    /// <param name="operation">The operation type</param>
    /// <param name="durationMs">The duration in milliseconds</param>
    public void RecordRepositoryDuration(string operation, double durationMs)
    {
        _repositoryOperationDuration.Record(durationMs,
            new KeyValuePair<string, object?>("operation", operation));
    }

    /// <summary>
    /// Records a SaveChanges call
    /// </summary>
    /// <param name="success">Whether the operation succeeded</param>
    public void RecordSaveChanges(bool success)
    {
        _saveChanges.Add(1, new KeyValuePair<string, object?>("status", success ? "success" : "failure"));
    }

    /// <summary>
    /// Records a transaction operation
    /// </summary>
    /// <param name="type">The transaction type (begin, commit, rollback)</param>
    public void RecordTransaction(string type)
    {
        _transactions.Add(1, new KeyValuePair<string, object?>("type", type));
    }

    /// <summary>
    /// Records a repository cache hit
    /// </summary>
    public void RecordCacheHit()
    {
        _cacheHits.Add(1);
    }

    /// <summary>
    /// Records a repository cache miss
    /// </summary>
    public void RecordCacheMiss()
    {
        _cacheMisses.Add(1);
    }

    /// <summary>
    /// Records entities processed by the audit interceptor
    /// </summary>
    /// <param name="state">The entity state (added, modified, deleted)</param>
    /// <param name="count">The number of entities</param>
    public void RecordAuditEntities(string state, int count = 1)
    {
        _auditEntities.Add(count, new KeyValuePair<string, object?>("state", state));
    }

    /// <summary>
    /// Records an ID generation
    /// </summary>
    /// <param name="keyType">The key type name</param>
    public void RecordIdGeneration(string keyType)
    {
        _idGeneration.Add(1, new KeyValuePair<string, object?>("key_type", keyType));
    }

    /// <summary>
    /// Records a domain event dispatch
    /// </summary>
    /// <param name="eventType">The event type name</param>
    public void RecordEventDispatched(string eventType)
    {
        _eventsDispatched.Add(1, new KeyValuePair<string, object?>("event_type", eventType));
    }

    /// <summary>
    /// Records a domain event handler error
    /// </summary>
    /// <param name="eventType">The event type name</param>
    /// <param name="handlerType">The handler type name</param>
    public void RecordEventHandlerError(string eventType, string handlerType)
    {
        _eventHandlerErrors.Add(1,
            new KeyValuePair<string, object?>("event_type", eventType),
            new KeyValuePair<string, object?>("handler_type", handlerType));
    }

    /// <summary>
    /// Records the duration of a domain event dispatch
    /// </summary>
    /// <param name="eventType">The event type name</param>
    /// <param name="durationMs">The duration in milliseconds</param>
    public void RecordEventDispatchDuration(string eventType, double durationMs)
    {
        _eventDispatchDuration.Record(durationMs,
            new KeyValuePair<string, object?>("event_type", eventType));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _meter.Dispose();
    }
}
