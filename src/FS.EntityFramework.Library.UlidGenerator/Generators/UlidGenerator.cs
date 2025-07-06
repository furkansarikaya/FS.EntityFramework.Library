using FS.EntityFramework.Library.Abstractions;

namespace FS.EntityFramework.Library.UlidGenerator.Generators;

/// <summary>
/// ULID generator that implements both generic and non-generic interfaces.
/// This design allows for both compile-time type safety and runtime flexibility.
/// </summary>
public class UlidGenerator : IIdGenerator<Ulid>
{
    /// <summary>
    /// Gets the type of key this generator produces (for runtime inspection)
    /// </summary>
    public Type KeyType => typeof(Ulid);

    /// <summary>
    /// Generates a new ULID with compile-time type safety
    /// </summary>
    /// <returns>A new ULID instance</returns>
    public Ulid Generate()
    {
        return Ulid.NewUlid();
    }

    /// <summary>
    /// Generates a new ULID and returns it as an object (for runtime usage)
    /// This method is called by the base interface when type is unknown at compile time
    /// </summary>
    /// <returns>A new ULID instance as an object</returns>
    object IIdGenerator.Generate()
    {
        // Simply call the typed version and return as object
        return Generate();
    }
}