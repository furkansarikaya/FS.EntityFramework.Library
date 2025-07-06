using FS.EntityFramework.Library.Abstractions;
using FS.EntityFramework.Library.Common;
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

    /// <summary>
    /// Initializes the interceptor with the ID generator factory
    /// </summary>
    /// <param name="idGeneratorFactory">Factory for resolving ID generators by type</param>
    public IdGenerationInterceptor(IIdGeneratorFactory idGeneratorFactory)
    {
        _idGeneratorFactory = idGeneratorFactory;
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
        
            var baseEntityType = GetBaseEntityType(entityType);
            if (baseEntityType == null) continue;

            var keyType = baseEntityType.GetGenericArguments()[0];
            var idProperty = entityType.GetProperty("Id");
        
            if (idProperty == null) continue;

            var currentValue = idProperty.GetValue(entity);

            if (!IsDefaultValue(currentValue, keyType)) continue;
            var generator = _idGeneratorFactory.GetGeneratorForType(keyType);
            if (generator == null) continue;
            var newId = generator.Generate();
                
            if (newId != null)
            {
                idProperty.SetValue(entity, newId);
            }
        }
    }

    /// <summary>
    /// Finds the BaseEntity<TKey> type in the inheritance hierarchy.
    /// This method walks up the inheritance chain to find our base entity.
    /// </summary>
    /// <param name="type">The entity type to examine</param>
    /// <returns>The BaseEntity<TKey> type or null if not found</returns>
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
    /// Checks if a value is the default value for its type.
    /// This determines whether we should generate a new ID.
    /// </summary>
    /// <param name="value">The current value</param>
    /// <param name="type">The type of the value</param>
    /// <returns>True if the value is the default for its type</returns>
    private static bool IsDefaultValue(object? value, Type type)
    {
        if (value == null) return true;
        
        // For value types, compare with the default value
        // For reference types, null is the default
        var defaultValue = type.IsValueType ? Activator.CreateInstance(type) : null;
        return Equals(value, defaultValue);
    }
}