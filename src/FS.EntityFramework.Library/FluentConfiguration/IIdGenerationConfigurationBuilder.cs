using FS.EntityFramework.Library.Abstractions;

namespace FS.EntityFramework.Library.FluentConfiguration;

/// <summary>
/// Configuration builder interface for ID generation functionality.
/// This provides the fluent API for registering custom ID generators.
/// </summary>
public interface IIdGenerationConfigurationBuilder
{
    /// <summary>
    /// Gets the parent FS.EntityFramework builder for method chaining
    /// </summary>
    IFSEntityFrameworkBuilder Builder { get; }
    
    /// <summary>
    /// Registers a custom ID generator for a specific type.
    /// This is the core method that allows extension packages to register their generators.
    /// </summary>
    /// <typeparam name="TKey">The key type this generator handles</typeparam>
    /// <typeparam name="TGenerator">The generator implementation</typeparam>
    /// <returns>The builder for method chaining</returns>
    IIdGenerationConfigurationBuilder WithGenerator<TKey, TGenerator>()
        where TKey : IEquatable<TKey>
        where TGenerator : class, IIdGenerator<TKey>;
    
    /// <summary>
    /// Registers a custom ID generator instance.
    /// Useful when you need to configure the generator with specific parameters.
    /// </summary>
    /// <typeparam name="TKey">The key type this generator handles</typeparam>
    /// <param name="generator">The configured generator instance</param>
    /// <returns>The builder for method chaining</returns>
    IIdGenerationConfigurationBuilder WithGenerator<TKey>(IIdGenerator<TKey> generator)
        where TKey : IEquatable<TKey>;
    
    /// <summary>
    /// Registers a generator using a factory function.
    /// This allows for complex generator creation logic.
    /// </summary>
    /// <typeparam name="TKey">The key type</typeparam>
    /// <param name="generatorFactory">Factory function to create the generator</param>
    /// <returns>The builder for method chaining</returns>
    IIdGenerationConfigurationBuilder WithGenerator<TKey>(Func<IServiceProvider, IIdGenerator<TKey>> generatorFactory)
        where TKey : IEquatable<TKey>;
    
    /// <summary>
    /// Completes the ID generation configuration and returns to the main builder
    /// </summary>
    /// <returns>The parent builder for method chaining</returns>
    IFSEntityFrameworkBuilder Complete();
}