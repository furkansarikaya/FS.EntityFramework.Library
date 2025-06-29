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

        return new FSEntityFrameworkBuilder(services, typeof(TContext));
    }

    /// <summary>
    /// Completes the fluent configuration and returns the service collection
    /// </summary>
    /// <param name="builder">The FS.EntityFramework builder</param>
    /// <returns>The service collection for further configuration</returns>
    public static IServiceCollection Build(this IFSEntityFrameworkBuilder builder)
    {
        return builder.Services;
    }
}
