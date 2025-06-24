using FS.EntityFramework.Library.Interceptors;
using FS.EntityFramework.Library.UnitOfWorks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FS.EntityFramework.Library;

public static class DependencyInjection
{
    /// <summary>
    /// Adds generic Unit of Work without audit interceptor
    /// </summary>
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
            return new AuditInterceptor(userProvider, timeProvider);
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
            return new AuditInterceptor(() => userContext.CurrentUser);
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