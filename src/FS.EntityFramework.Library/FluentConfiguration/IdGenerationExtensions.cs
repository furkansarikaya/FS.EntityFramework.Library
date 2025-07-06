using FS.EntityFramework.Library.Abstractions;
using FS.EntityFramework.Library.Factories;
using FS.EntityFramework.Library.Interceptors;
using Microsoft.Extensions.DependencyInjection;

namespace FS.EntityFramework.Library.FluentConfiguration;

/// <summary>
/// Extensions for configuring ID generation strategies.
/// This is the entry point for the ID generation functionality.
/// </summary>
public static class IdGenerationExtensions
{
    /// <summary>
    /// Configures automatic ID generation for entities.
    /// This method sets up the core infrastructure needed for ID generation.
    /// </summary>
    /// <param name="builder">The FS.EntityFramework builder</param>
    /// <returns>ID generation configuration builder for further customization</returns>
    public static IIdGenerationConfigurationBuilder WithIdGeneration(this IFSEntityFrameworkBuilder builder)
    {
        // Register the factory service - this is the central registry for all generators
        // The factory will be used by the interceptor to find the right generator for each entity type
        builder.Services.AddScoped<IIdGeneratorFactory, IdGeneratorFactory>();
        
        // Register the interceptor that handles automatic ID generation during SaveChanges
        // This interceptor will examine all new entities and generate IDs where appropriate
        builder.Services.AddScoped<IdGenerationInterceptor>();
        
        // Return the configuration builder so users can register specific generators
        return new IdGenerationConfigurationBuilder(builder);
    }
}