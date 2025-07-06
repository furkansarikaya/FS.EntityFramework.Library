using FS.EntityFramework.Library.Abstractions;

namespace FS.EntityFramework.Library.GuidV7.Generators;

/// <summary>
/// GUID Version 7 generator with custom timestamp support
/// </summary>
public class TimestampedGuidV7Generator : IIdGenerator<Guid>
{
    private readonly Func<DateTimeOffset> _timestampProvider;

    /// <summary>
    /// Initializes a new instance with custom timestamp provider
    /// </summary>
    /// <param name="timestampProvider">Function to provide timestamp for GUID generation</param>
    public TimestampedGuidV7Generator(Func<DateTimeOffset> timestampProvider)
    {
        _timestampProvider = timestampProvider;
    }
    
    /// <summary>
    /// Gets the type of key this generator produces
    /// </summary>
    public Type KeyType => typeof(Guid);

    /// <summary>
    /// Generates a new GUID Version 7 using the provided timestamp
    /// </summary>
    /// <returns>A new GUID Version 7 instance</returns>
    public Guid Generate()
    {
        return Guid.CreateVersion7(_timestampProvider());
    }

    /// <summary>
    /// Non-generic implementation for runtime usage
    /// </summary>
    /// <returns>A new GUID Version 7 as an object</returns>
    object IIdGenerator.Generate()
    {
        return Generate();
    }
}