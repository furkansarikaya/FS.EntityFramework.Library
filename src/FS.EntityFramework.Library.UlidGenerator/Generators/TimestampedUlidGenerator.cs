using FS.EntityFramework.Library.Abstractions;

namespace FS.EntityFramework.Library.UlidGenerator.Generators;

/// <summary>
/// ULID generator with custom timestamp support
/// </summary>
public class TimestampedUlidGenerator : IIdGenerator<Ulid>
{
    private readonly Func<DateTimeOffset> _timestampProvider;

    /// <summary>
    /// Initializes a new instance with custom timestamp provider
    /// </summary>
    /// <param name="timestampProvider">Function to provide timestamp for ULID generation</param>
    public TimestampedUlidGenerator(Func<DateTimeOffset> timestampProvider)
    {
        _timestampProvider = timestampProvider;
    }
    
    /// <summary>
    /// Gets the type of key this generator produces (for runtime inspection)
    /// </summary>
    public Type KeyType => typeof(Ulid);

    /// <summary>
    /// Generates a new ULID using the provided timestamp
    /// </summary>
    /// <returns>A new ULID instance</returns>
    public Ulid Generate()
    {
        return Ulid.NewUlid(_timestampProvider());
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