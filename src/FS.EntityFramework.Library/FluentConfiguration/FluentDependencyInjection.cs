using FS.EntityFramework.Library.Interceptors;
using FS.EntityFramework.Library.UnitOfWorks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FS.EntityFramework.Library.FluentConfiguration;

/// <summary>
/// Main entry point for fluent configuration of FS.EntityFramework.Library services
/// </summary>
public static class FluentDependencyInjection
{
    /// <summary>
    /// Adds FS.EntityFramework.Library services with fluent configuration support
    /// </summary>
    /// <typeparam name="TContext">The DbContext type</typeparam>
    /// <param name="services">The service collection</param>
    /// <returns>The fluent configuration builder for further configuration</returns>
    public static IFSEntityFrameworkBuilder AddFSEntityFramework<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        // Register core Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>(provider =>
        {
            var context = provider.GetRequiredService<TContext>();
            return new UnitOfWork(context, provider);
        });

        // Register fluent configuration applier
        services.AddScoped<IFluentConfigurationApplier, FluentConfigurationApplier>();

        return new FSEntityFrameworkBuilder(services, typeof(TContext));
    }

    /// <summary>
    /// Completes the fluent configuration and automatically configures interceptors
    /// </summary>
    /// <param name="builder">The FS.EntityFramework builder</param>
    /// <returns>The service collection for further configuration</returns>
    public static IServiceCollection Build(this IFSEntityFrameworkBuilder builder)
    {
        // Automatically configure DbContext to use interceptors
        ConfigureDbContextInterceptors(builder);
        
        return builder.Services;
    }

    /// <summary>
    /// Configures the DbContext to automatically include registered interceptors
    /// </summary>
    /// <param name="builder">The FS.EntityFramework builder</param>
    private static void ConfigureDbContextInterceptors(IFSEntityFrameworkBuilder builder)
    {
        var dbContextType = builder.DbContextType;
        
        // Find existing DbContext registration
        var existingRegistration = builder.Services.FirstOrDefault(x => x.ServiceType == dbContextType);
        if (existingRegistration == null) return;

        // Remove existing registration
        builder.Services.Remove(existingRegistration);

        // Add new registration that automatically includes interceptors
        builder.Services.Add(new ServiceDescriptor(
            dbContextType,
            serviceProvider => CreateDbContextWithInterceptors(serviceProvider, dbContextType, existingRegistration),
            existingRegistration.Lifetime));
    }

    /// <summary>
    /// Creates a DbContext instance with automatically configured interceptors
    /// </summary>
    /// <param name="serviceProvider">The service provider</param>
    /// <param name="dbContextType">The DbContext type to create</param>
    /// <param name="originalRegistration">The original service registration</param>
    /// <returns>A DbContext instance with interceptors</returns>
    private static object CreateDbContextWithInterceptors(
        IServiceProvider serviceProvider, 
        Type dbContextType, 
        ServiceDescriptor originalRegistration)
    {
        // Get the original DbContextOptions
        var optionsType = typeof(DbContextOptions<>).MakeGenericType(dbContextType);
        var options = serviceProvider.GetService(optionsType) as DbContextOptions;
        
        if (options == null)
        {
            // Fallback: use original factory if available
            if (originalRegistration.ImplementationFactory != null)
            {
                return originalRegistration.ImplementationFactory(serviceProvider);
            }
            throw new InvalidOperationException($"Cannot resolve DbContextOptions for {dbContextType.Name}");
        }

        // Create new options with interceptors
        var optionsBuilder = new DbContextOptionsBuilder(options);
        AddInterceptorsToOptions(optionsBuilder, serviceProvider);

        // Create DbContext instance with fallback mechanism
        try
        {
            // Try with ServiceProvider parameter first (for FSDbContext)
            return Activator.CreateInstance(dbContextType, optionsBuilder.Options, serviceProvider)!;
        }
        catch
        {
            try
            {
                // Fallback: try without ServiceProvider parameter
                return Activator.CreateInstance(dbContextType, optionsBuilder.Options)!;
            }
            catch
            {
                // Last resort: use original factory if available
                if (originalRegistration.ImplementationFactory != null)
                {
                    return originalRegistration.ImplementationFactory(serviceProvider);
                }
                throw;
            }
        }
    }

    /// <summary>
    /// Adds registered interceptors to the DbContext options
    /// </summary>
    /// <param name="optionsBuilder">The DbContext options builder</param>
    /// <param name="serviceProvider">The service provider</param>
    private static void AddInterceptorsToOptions(DbContextOptionsBuilder optionsBuilder, IServiceProvider serviceProvider)
    {
        var interceptors = new List<Microsoft.EntityFrameworkCore.Diagnostics.IInterceptor>();

        // Add audit interceptor if registered
        var auditInterceptor = serviceProvider.GetService<AuditInterceptor>();
        if (auditInterceptor != null)
        {
            interceptors.Add(auditInterceptor);
        }

        // Add domain event interceptor if registered
        var domainEventInterceptor = serviceProvider.GetService<DomainEventInterceptor>();
        if (domainEventInterceptor != null)
        {
            interceptors.Add(domainEventInterceptor);
        }

        // Add interceptors to options if any were found
        if (interceptors.Count > 0)
        {
            optionsBuilder.AddInterceptors(interceptors.ToArray());
        }
    }
}