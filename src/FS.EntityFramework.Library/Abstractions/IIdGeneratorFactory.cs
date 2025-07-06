namespace FS.EntityFramework.Library.Abstractions;

public interface IIdGeneratorFactory
{
    /// <summary>
    /// Gets a type-safe ID generator for compile-time usage.
    /// This method maintains all generic constraints and provides full type safety.
    /// </summary>
    /// <typeparam name="TKey">The type of identifier (must implement IEquatable)</typeparam>
    /// <returns>A strongly-typed ID generator or null if not registered</returns>
    IIdGenerator<TKey>? GetGenerator<TKey>() where TKey : IEquatable<TKey>;
    
    /// <summary>
    /// Gets an ID generator for runtime usage when type is only known at runtime.
    /// This method returns the base interface which can generate IDs as objects.
    /// The returned generator is guaranteed to produce IDs of the specified type.
    /// </summary>
    /// <param name="keyType">The runtime type of the key (must implement IEquatable)</param>
    /// <returns>A base ID generator or null if not registered or if type doesn't satisfy constraints</returns>
    IIdGenerator? GetGeneratorForType(Type keyType);
}