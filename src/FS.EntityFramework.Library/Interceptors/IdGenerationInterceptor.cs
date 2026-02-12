using System.Collections.Concurrent;
using System.Reflection;
using FS.EntityFramework.Library.Abstractions;
using FS.EntityFramework.Library.Common;
using FS.EntityFramework.Library.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FS.EntityFramework.Library.Interceptors;

/// <summary>
/// Interceptor that automatically generates IDs for new entities using registered generators.
/// This is where the automatic ID generation magic happens.
/// </summary>
public class IdGenerationInterceptor : SaveChangesInterceptor
{
    private readonly IIdGeneratorFactory _idGeneratorFactory;
    private readonly FSEntityFrameworkMetrics? _metrics;

    /// <summary>
    /// Cache for BaseEntity type hierarchy lookups
    /// </summary>
    private static readonly ConcurrentDictionary<Type, Type?> BaseEntityTypeCache = new();

    /// <summary>
    /// Cache for Id PropertyInfo lookups
    /// </summary>
    private static readonly ConcurrentDictionary<Type, PropertyInfo?> IdPropertyCache = new();

    /// <summary>
    /// Initializes the interceptor with the ID generator factory
    /// </summary>
    /// <param name="idGeneratorFactory">Factory for resolving ID generators by type</param>
    /// <param name="metrics">Optional metrics instance</param>
    public IdGenerationInterceptor(IIdGeneratorFactory idGeneratorFactory, FSEntityFrameworkMetrics? metrics = null)
    {
        _idGeneratorFactory = idGeneratorFactory;
        _metrics = metrics;
    }

    /// <summary>
    /// Synchronous save changes interception
    /// </summary>
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        GenerateIds(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <summary>
    /// Asynchronous save changes interception
    /// </summary>
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, 
        InterceptionResult<int> result, 
        CancellationToken cancellationToken = default)
    {
        GenerateIds(eventData.Context);
        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <summary>
    /// The core logic that examines new entities and generates IDs where appropriate.
    /// This method demonstrates the power of our modular architecture.
    /// </summary>
    /// <param name="context">The DbContext being saved</param>
    private void GenerateIds(DbContext? context)
    {
        if (context == null) return;

        var addedEntries = context.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added)
            .ToList();

        foreach (var entry in addedEntries)
        {
            var entity = entry.Entity;
            var entityType = entity.GetType();

            var baseEntityType = BaseEntityTypeCache.GetOrAdd(entityType, static t => GetBaseEntityType(t));
            if (baseEntityType == null) continue;

            var keyType = baseEntityType.GetGenericArguments()[0];
            var idProperty = IdPropertyCache.GetOrAdd(entityType, static t => t.GetProperty("Id"));

            if (idProperty == null) continue;

            var currentValue = idProperty.GetValue(entity);

            // FIXED: Improved default value detection for nullable types
            if (!IsDefaultValue(currentValue, keyType)) continue;

            var generator = _idGeneratorFactory.GetGeneratorForType(keyType);
            if (generator == null) continue;

            var newId = generator.Generate();

            if (newId != null)
            {
                idProperty.SetValue(entity, newId);
                _metrics?.RecordIdGeneration(keyType.Name);
            }
        }
    }

    /// <summary>
    /// Finds the BaseEntity type in the inheritance hierarchy.
    /// This method walks up the inheritance chain to find our base entity.
    /// </summary>
    /// <param name="type">The entity type to examine</param>
    /// <returns>The BaseEntity type or null if not found</returns>
    private static Type? GetBaseEntityType(Type type)
    {
        var current = type;
        while (current != null)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(BaseEntity<>))
            {
                return current;
            }
            current = current.BaseType;
        }
        return null;
    }

    /// <summary>
    /// FIXED: Enhanced default value detection that properly handles nullable types
    /// This determines whether we should generate a new ID.
    /// </summary>
    /// <param name="value">The current value</param>
    /// <param name="type">The type of the value</param>
    /// <returns>True if the value is the default for its type</returns>
    private static bool IsDefaultValue(object? value, Type type)
    {
        // Handle null values first
        if (value == null)
        {
            // For reference types and nullable value types, null is indeed default
            return !type.IsValueType || Nullable.GetUnderlyingType(type) != null;
        }

        // CRITICAL FIX: Handle nullable value types properly
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        // Fast path for common types (avoids boxing via Activator.CreateInstance)
        if (underlyingType == typeof(int)) return (int)value == 0;
        if (underlyingType == typeof(long)) return (long)value == 0L;
        if (underlyingType == typeof(Guid)) return (Guid)value == Guid.Empty;
        if (underlyingType == typeof(string)) return value is null or "";

        // Fallback for other types
        var defaultValue = GetDefaultValue(underlyingType);
        return Equals(value, defaultValue);
    }

    /// <summary>
    /// Helper method to get the actual default value for a type
    /// This handles both value types and reference types correctly
    /// </summary>
    /// <param name="type">The type to get default value for</param>
    /// <returns>The default value for the type</returns>
    private static object? GetDefaultValue(Type type)
    {
        // For reference types, default is null
        return !type.IsValueType ? null :
            // For value types, use Activator.CreateInstance to get default
            Activator.CreateInstance(type);
    }
}