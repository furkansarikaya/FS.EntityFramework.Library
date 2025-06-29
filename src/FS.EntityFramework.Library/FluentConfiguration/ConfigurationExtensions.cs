using Microsoft.Extensions.DependencyInjection;

namespace FS.EntityFramework.Library.FluentConfiguration;

/// <summary>
/// Additional configuration extensions for advanced scenarios
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// Adds custom interceptors to the configuration
    /// </summary>
    /// <param name="builder">The FS.EntityFramework builder</param>
    /// <param name="serviceLifetime">The service lifetime for the interceptor</param>
    /// <returns>The builder for method chaining</returns>
    public static IFSEntityFrameworkBuilder WithInterceptor<TInterceptor>(
        this IFSEntityFrameworkBuilder builder,
        ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
        where TInterceptor : class
    {
        builder.Services.Add(new ServiceDescriptor(typeof(TInterceptor), typeof(TInterceptor), serviceLifetime));
        return builder;
    }

    /// <summary>
    /// Adds custom services to the configuration
    /// </summary>
    /// <param name="builder">The FS.EntityFramework builder</param>
    /// <param name="configureServices">Action to configure additional services</param>
    /// <returns>The builder for method chaining</returns>
    public static IFSEntityFrameworkBuilder WithServices(
        this IFSEntityFrameworkBuilder builder,
        Action<IServiceCollection> configureServices)
    {
        configureServices(builder.Services);
        return builder;
    }

    /// <summary>
    /// Conditionally configures the builder based on a predicate
    /// </summary>
    /// <param name="builder">The FS.EntityFramework builder</param>
    /// <param name="condition">The condition to check</param>
    /// <param name="configure">Action to configure if condition is true</param>
    /// <returns>The builder for method chaining</returns>
    public static IFSEntityFrameworkBuilder When(
        this IFSEntityFrameworkBuilder builder,
        bool condition,
        Action<IFSEntityFrameworkBuilder> configure)
    {
        if (condition)
        {
            configure(builder);
        }
        return builder;
    }

    /// <summary>
    /// Conditionally configures the builder based on a predicate function
    /// </summary>
    /// <param name="builder">The FS.EntityFramework builder</param>
    /// <param name="condition">The condition function to check</param>
    /// <param name="configure">Action to configure if condition is true</param>
    /// <returns>The builder for method chaining</returns>
    public static IFSEntityFrameworkBuilder When(
        this IFSEntityFrameworkBuilder builder,
        Func<IFSEntityFrameworkBuilder, bool> condition,
        Action<IFSEntityFrameworkBuilder> configure)
    {
        if (condition(builder))
        {
            configure(builder);
        }
        return builder;
    }
}