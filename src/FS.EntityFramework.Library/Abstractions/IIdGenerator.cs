namespace FS.EntityFramework.Library.Abstractions;

/// <summary>
/// Non-generic base interface for ID generators.
/// This allows us to handle generators at runtime without losing type safety.
/// </summary>
public interface IIdGenerator
{
    /// <summary>
    /// Generates a new ID and returns it as an object.
    /// The actual type will be the TKey type that the concrete generator handles.
    /// </summary>
    /// <returns>A newly generated ID of the appropriate type</returns>
    object Generate();
    
    /// <summary>
    /// Gets the type of ID that this generator produces.
    /// This is useful for validation and type checking.
    /// </summary>
    Type KeyType { get; }
}

/// <summary>
/// Generic interface for generating unique identifiers for entities.
/// This provides compile-time type safety while extending the base interface.
/// </summary>
/// <typeparam name="TKey">The type of identifier to generate</typeparam>
public interface IIdGenerator<out TKey> : IIdGenerator where TKey : IEquatable<TKey>
{
    /// <summary>
    /// Generates a new unique identifier with compile-time type safety.
    /// </summary>
    /// <returns>A new unique identifier of type TKey</returns>
    new TKey Generate(); // 'new' keyword shadows the base method with proper typing
}