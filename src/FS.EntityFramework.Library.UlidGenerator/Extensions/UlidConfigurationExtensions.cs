using FS.EntityFramework.Library.Abstractions;
using FS.EntityFramework.Library.FluentConfiguration;
using FS.EntityFramework.Library.UlidGenerator.Generators;
using Microsoft.Extensions.DependencyInjection;

namespace FS.EntityFramework.Library.UlidGenerator.Extensions;

/// <summary>
/// Fluent configuration extensions for ULID ID generation
/// </summary>
public static class UlidConfigurationExtensions
{
    /// <summary>
    /// Configures automatic ULID generation for entities
    /// </summary>
    /// <param name="builder">The FS.EntityFramework builder</param>
    /// <returns>The builder for method chaining</returns>
    public static IFSEntityFrameworkBuilder WithUlid(this IFSEntityFrameworkBuilder builder)
    {
        // Add ID generation support if not already added
        var idGenerationBuilder = builder.WithIdGeneration();
        
        // Register ULID generator
        builder.Services.AddScoped<IIdGenerator<Ulid>, Generators.UlidGenerator>();
        
        return idGenerationBuilder.Complete();
    }

    /// <summary>
    /// Configures ULID generation with custom timestamp provider
    /// </summary>
    /// <param name="builder">The FS.EntityFramework builder</param>
    /// <param name="timestampProvider">Function to provide timestamp for ULID generation</param>
    /// <returns>The builder for method chaining</returns>
    public static IFSEntityFrameworkBuilder WithUlid(
        this IFSEntityFrameworkBuilder builder,
        Func<DateTimeOffset> timestampProvider)
    {
        var idGenerationBuilder = builder.WithIdGeneration();
        
        // Register custom timestamped ULID generator
        builder.Services.AddScoped<IIdGenerator<Ulid>>(
            _ => new TimestampedUlidGenerator(timestampProvider));
        
        return idGenerationBuilder.Complete();
    }

    /// <summary>
    /// Configures ULID generation with service provider based timestamp
    /// </summary>
    /// <param name="builder">The FS.EntityFramework builder</param>
    /// <param name="timestampProvider">Function to provide timestamp using service provider</param>
    /// <returns>The builder for method chaining</returns>
    public static IFSEntityFrameworkBuilder WithUlid(
        this IFSEntityFrameworkBuilder builder,
        Func<IServiceProvider, DateTimeOffset> timestampProvider)
    {
        var idGenerationBuilder = builder.WithIdGeneration();
        
        builder.Services.AddScoped<IIdGenerator<Ulid>>(
            provider => new TimestampedUlidGenerator(() => timestampProvider(provider)));
        
        return idGenerationBuilder.Complete();
    }
}