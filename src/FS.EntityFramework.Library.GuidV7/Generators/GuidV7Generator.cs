using FS.EntityFramework.Library.Abstractions;

namespace FS.EntityFramework.Library.GuidV7.Generators;

/// <summary>
/// GUID Version 7 generator implementing both interfaces for maximum flexibility
/// </summary>
public class GuidV7Generator : IIdGenerator<Guid>
{
    /// <summary>
    /// Gets the type of key this generator produces
    /// </summary>
    public Type KeyType => typeof(Guid);

    /// <summary>
    /// Generates a new GUID Version 7 with compile-time type safety
    /// </summary>
    /// <returns>A new GUID Version 7 instance</returns>
    public Guid Generate()
    {
        return Guid.CreateVersion7();
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