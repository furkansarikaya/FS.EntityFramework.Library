using FS.EntityFramework.Library.Abstractions;
using FS.EntityFramework.Library.FluentConfiguration;
using FS.EntityFramework.Library.GuidV7.Generators;
using Microsoft.Extensions.DependencyInjection;

namespace FS.EntityFramework.Library.GuidV7.Extensions;

/// <summary>
/// Fluent configuration extensions for GUID Version 7 ID generation
/// </summary>
public static class GuidV7ConfigurationExtensions
{
    /// <summary>
    /// Configures automatic GUID Version 7 generation for entities
    /// Requires .NET 9+ for native Guid.CreateVersion7() support
    /// </summary>
    /// <param name="builder">The FS.EntityFramework builder</param>
    /// <returns>The builder for method chaining</returns>
    public static IFSEntityFrameworkBuilder WithGuidV7(this IFSEntityFrameworkBuilder builder)
    {
        // Add ID generation support if not already added
        var idGenerationBuilder = builder.WithIdGeneration();
        
        // Register GUID V7 generator
        builder.Services.AddScoped<IIdGenerator<Guid>, GuidV7Generator>();
        
        return idGenerationBuilder.Complete();
    }

    /// <summary>
    /// Configures GUID Version 7 generation with custom timestamp provider
    /// </summary>
    /// <param name="builder">The FS.EntityFramework builder</param>
    /// <param name="timestampProvider">Function to provide timestamp for GUID generation</param>
    /// <returns>The builder for method chaining</returns>
    public static IFSEntityFrameworkBuilder WithGuidV7(
        this IFSEntityFrameworkBuilder builder,
        Func<DateTimeOffset> timestampProvider)
    {
        var idGenerationBuilder = builder.WithIdGeneration();
        
        // Register custom timestamped GUID V7 generator
        builder.Services.AddScoped<IIdGenerator<Guid>>(
            _ => new TimestampedGuidV7Generator(timestampProvider));
        
        return idGenerationBuilder.Complete();
    }

    /// <summary>
    /// Configures GUID Version 7 generation with service provider based timestamp
    /// </summary>
    /// <param name="builder">The FS.EntityFramework builder</param>
    /// <param name="timestampProvider">Function to provide timestamp using service provider</param>
    /// <returns>The builder for method chaining</returns>
    public static IFSEntityFrameworkBuilder WithGuidV7(
        this IFSEntityFrameworkBuilder builder,
        Func<IServiceProvider, DateTimeOffset> timestampProvider)
    {
        var idGenerationBuilder = builder.WithIdGeneration();
        
        builder.Services.AddScoped<IIdGenerator<Guid>>(
            provider => new TimestampedGuidV7Generator(() => timestampProvider(provider)));
        
        return idGenerationBuilder.Complete();
    }
}