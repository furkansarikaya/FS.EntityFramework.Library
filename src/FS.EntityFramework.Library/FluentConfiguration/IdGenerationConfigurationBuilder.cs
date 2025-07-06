using FS.EntityFramework.Library.Abstractions;
using FS.EntityFramework.Library.Factories;
using FS.EntityFramework.Library.Interceptors;
using Microsoft.Extensions.DependencyInjection;

namespace FS.EntityFramework.Library.FluentConfiguration;

/// <summary>
/// Internal implementation of the ID generation configuration builder.
/// This class handles the actual registration of generators in the DI container.
/// </summary>
internal class IdGenerationConfigurationBuilder : IIdGenerationConfigurationBuilder
{
    /// <summary>
    /// Initializes a new instance with the parent builder
    /// </summary>
    /// <param name="builder">The parent FS.EntityFramework builder</param>
    public IdGenerationConfigurationBuilder(IFSEntityFrameworkBuilder builder)
    {
        Builder = builder;
    }

    /// <summary>
    /// Gets the parent FS.EntityFramework builder
    /// </summary>
    public IFSEntityFrameworkBuilder Builder { get; }

    /// <summary>
    /// Registers a custom ID generator type in the DI container.
    /// This is the fundamental registration method that other methods build upon.
    /// </summary>
    /// <typeparam name="TKey">The key type this generator handles</typeparam>
    /// <typeparam name="TGenerator">The generator implementation</typeparam>
    /// <returns>The builder for method chaining</returns>
    public IIdGenerationConfigurationBuilder WithGenerator<TKey, TGenerator>()
        where TKey : IEquatable<TKey>
        where TGenerator : class, IIdGenerator<TKey>
    {
        // Register the generator in the DI container
        // This creates the mapping: IIdGenerator<TKey> -> TGenerator
        Builder.Services.AddScoped<IIdGenerator<TKey>, TGenerator>();
        
        return this;
    }

    /// <summary>
    /// Registers a pre-configured generator instance.
    /// This is useful when the generator needs specific configuration that can't be done through DI.
    /// </summary>
    /// <typeparam name="TKey">The key type this generator handles</typeparam>
    /// <param name="generator">The configured generator instance</param>
    /// <returns>The builder for method chaining</returns>
    public IIdGenerationConfigurationBuilder WithGenerator<TKey>(IIdGenerator<TKey> generator)
        where TKey : IEquatable<TKey>
    {
        // Register the specific instance
        // This creates a singleton registration with the provided instance
        Builder.Services.AddSingleton<IIdGenerator<TKey>>(generator);
        
        return this;
    }

    /// <summary>
    /// Registers a generator using a factory function.
    /// This provides maximum flexibility for complex generator creation scenarios.
    /// </summary>
    /// <typeparam name="TKey">The key type</typeparam>
    /// <param name="generatorFactory">Factory function to create the generator</param>
    /// <returns>The builder for method chaining</returns>
    public IIdGenerationConfigurationBuilder WithGenerator<TKey>(Func<IServiceProvider, IIdGenerator<TKey>> generatorFactory)
        where TKey : IEquatable<TKey>
    {
        // Register the factory function
        // The DI container will call this function when an IIdGenerator<TKey> is requested
        Builder.Services.AddScoped<IIdGenerator<TKey>>(generatorFactory);
        
        return this;
    }

    /// <summary>
    /// Completes the ID generation configuration.
    /// This method ensures all necessary services are registered and returns control to the main builder.
    /// </summary>
    /// <returns>The parent builder for method chaining</returns>
    public IFSEntityFrameworkBuilder Complete()
    {
        // The core services should already be registered when WithIdGeneration() was called
        // But we can do validation here if needed
        
        // Ensure the factory is registered (defensive programming)
        var factoryDescriptor = Builder.Services.FirstOrDefault(x => x.ServiceType == typeof(IIdGeneratorFactory));
        if (factoryDescriptor == null)
        {
            Builder.Services.AddScoped<IIdGeneratorFactory, IdGeneratorFactory>();
        }
        
        // Ensure the interceptor is registered
        var interceptorDescriptor = Builder.Services.FirstOrDefault(x => x.ServiceType == typeof(IdGenerationInterceptor));
        if (interceptorDescriptor == null)
        {
            Builder.Services.AddScoped<IdGenerationInterceptor>();
        }
        
        return Builder;
    }
}