using FS.EntityFramework.Library.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace FS.EntityFramework.Library.Factories;

/// <summary>
/// Factory implementation that ensures type safety and constraint validation.
/// </summary>
public class IdGeneratorFactory : IIdGeneratorFactory
{
    private readonly IServiceProvider _serviceProvider;

    public IdGeneratorFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Type-safe generator resolution for compile-time usage
    /// </summary>
    public IIdGenerator<TKey>? GetGenerator<TKey>() where TKey : IEquatable<TKey>
    {
        // This is straightforward - constraints are enforced at compile time
        return _serviceProvider.GetService<IIdGenerator<TKey>>();
    }

    /// <summary>
    /// Runtime generator resolution with constraint validation.
    /// This method ensures that only types satisfying our constraints can get generators.
    /// </summary>
    public IIdGenerator? GetGeneratorForType(Type keyType)
    {
        // First, validate that the type satisfies our constraints
        if (!IsValidKeyType(keyType))
        {
            // If the type doesn't satisfy IEquatable<T>, we return null
            // This prevents constraint violations at runtime
            return null;
        }

        // Build the generic interface type: IIdGenerator<keyType>
        var generatorInterfaceType = typeof(IIdGenerator<>).MakeGenericType(keyType);
        
        // Resolve the generator from DI container
        var generator = _serviceProvider.GetService(generatorInterfaceType);
        
        // Cast to base interface for non-generic usage
        return generator as IIdGenerator;
    }

    /// <summary>
    /// Validates that a type satisfies the IEquatable constraint.
    /// This is crucial for maintaining type safety at runtime.
    /// </summary>
    /// <param name="keyType">The type to validate</param>
    /// <returns>True if the type implements IEquatable&lt;T&gt; of itself</returns>
    private static bool IsValidKeyType(Type keyType)
    {
        // Check if the type implements IEquatable<keyType>
        var equatableInterface = typeof(IEquatable<>).MakeGenericType(keyType);
        return equatableInterface.IsAssignableFrom(keyType);
    }
}