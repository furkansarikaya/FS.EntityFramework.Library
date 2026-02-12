using FS.EntityFramework.Library.Diagnostics;
using FS.EntityFramework.Library.Interceptors;
using FS.EntityFramework.Library.UnitOfWorks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FS.EntityFramework.Library;

/// <summary>
/// Extension methods for configuring dependency injection for the Entity Framework library
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds the generic Unit of Work pattern implementation to the service collection
    /// </summary>
    /// <typeparam name="TContext">The DbContext type</typeparam>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddGenericUnitOfWork<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        services.AddScoped<IUnitOfWork, UnitOfWork>(provider =>
        {
            var context = provider.GetRequiredService<TContext>();
            return new UnitOfWork(context, provider);
        });

        return services;
    }

    /// <summary>
    /// Adds generic Unit of Work with audit interceptor using delegate
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="getCurrentUser">Function to get current user identifier</param>
    /// <param name="getCurrentTime">Function to get current time (optional)</param>
    public static IServiceCollection AddGenericUnitOfWorkWithAudit<TContext>(
        this IServiceCollection services,
        Func<IServiceProvider, string?> getCurrentUser,
        Func<IServiceProvider, DateTime>? getCurrentTime = null)
        where TContext : DbContext
    {
        // Add audit interceptor
        services.AddScoped<AuditInterceptor>(provider =>
        {
            var userProvider = () => getCurrentUser(provider);
            Func<DateTime>? timeProvider = getCurrentTime != null ? () => getCurrentTime(provider) : null;
            var metrics = provider.GetService<FSEntityFrameworkMetrics>();
            return new AuditInterceptor(userProvider, timeProvider, metrics);
        });

        // Add Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>(provider =>
        {
            var context = provider.GetRequiredService<TContext>();
            return new UnitOfWork(context, provider);
        });

        return services;
    }

    /// <summary>
    /// Adds generic Unit of Work with audit interceptor using interface
    /// </summary>
    public static IServiceCollection AddGenericUnitOfWorkWithAudit<TContext, TUserContext>(
        this IServiceCollection services)
        where TContext : DbContext
        where TUserContext : class, IUserContext
    {
        // Add audit interceptor
        services.AddScoped<AuditInterceptor>(provider =>
        {
            var userContext = provider.GetRequiredService<TUserContext>();
            var metrics = provider.GetService<FSEntityFrameworkMetrics>();
            return new AuditInterceptor(() => userContext.CurrentUser, metrics: metrics);
        });

        // Add Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>(provider =>
        {
            var context = provider.GetRequiredService<TContext>();
            return new UnitOfWork(context, provider);
        });

        return services;
    }
}

/// <summary>
/// Optional interface for user context - implement this if you prefer interface-based approach
/// </summary>
public interface IUserContext
{
    string? CurrentUser { get; }
}