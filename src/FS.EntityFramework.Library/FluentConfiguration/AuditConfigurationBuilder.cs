using FS.EntityFramework.Library.Diagnostics;
using FS.EntityFramework.Library.Interceptors;
using Microsoft.Extensions.DependencyInjection;

namespace FS.EntityFramework.Library.FluentConfiguration;

/// <summary>
/// Internal implementation of the audit configuration builder
/// </summary>
internal class AuditConfigurationBuilder : IAuditConfigurationBuilder
{
    /// <summary>
    /// Initializes a new instance of the AuditConfigurationBuilder class
    /// </summary>
    /// <param name="builder">The parent FS.EntityFramework builder</param>
    public AuditConfigurationBuilder(IFSEntityFrameworkBuilder builder)
    {
        Builder = builder;
    }

    /// <summary>
    /// Gets the parent FS.EntityFramework builder
    /// </summary>
    public IFSEntityFrameworkBuilder Builder { get; }

    /// <summary>
    /// Configures the user provider using a delegate function
    /// </summary>
    /// <param name="getCurrentUser">Function to get the current user identifier</param>
    /// <param name="getCurrentTime">Optional function to get the current time (defaults to UTC now)</param>
    /// <returns>The parent builder for method chaining</returns>
    public IFSEntityFrameworkBuilder UsingUserProvider(
        Func<IServiceProvider, string?> getCurrentUser,
        Func<IServiceProvider, DateTime>? getCurrentTime = null)
    {
        Builder.Services.AddScoped<AuditInterceptor>(provider =>
        {
            var userProvider = () => getCurrentUser(provider);
            Func<DateTime>? timeProvider = getCurrentTime != null ? () => getCurrentTime(provider) : null;
            var metrics = provider.GetService<FSEntityFrameworkMetrics>();
            return new AuditInterceptor(userProvider, timeProvider, metrics);
        });

        return Builder;
    }

    /// <summary>
    /// Configures the user provider using an interface-based approach
    /// </summary>
    /// <typeparam name="TUserContext">The user context interface implementation</typeparam>
    /// <returns>The parent builder for method chaining</returns>
    public IFSEntityFrameworkBuilder UsingUserContext<TUserContext>()
        where TUserContext : class, IUserContext
    {
        Builder.Services.AddScoped<AuditInterceptor>(provider =>
        {
            var userContext = provider.GetRequiredService<TUserContext>();
            var metrics = provider.GetService<FSEntityFrameworkMetrics>();
            return new AuditInterceptor(() => userContext.CurrentUser, metrics: metrics);
        });

        return Builder;
    }

    /// <summary>
    /// Configures the user provider using HttpContext for web applications
    /// </summary>
    /// <param name="claimType">The claim type to extract user ID from</param>
    /// <returns>The parent builder for method chaining</returns>
    public IFSEntityFrameworkBuilder UsingHttpContext(string claimType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")
    {
        Builder.Services.AddHttpContextAccessor();

        Builder.Services.AddScoped<AuditInterceptor>(provider =>
        {
            var httpContextAccessor = provider.GetRequiredService<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
            var metrics = provider.GetService<FSEntityFrameworkMetrics>();
            return new AuditInterceptor(() =>
                httpContextAccessor.HttpContext?.User?.FindFirst(claimType)?.Value, metrics: metrics);
        });

        return Builder;
    }

    /// <summary>
    /// Configures the user provider using a static user ID (typically for testing)
    /// </summary>
    /// <param name="userId">The static user ID to use</param>
    /// <returns>The parent builder for method chaining</returns>
    public IFSEntityFrameworkBuilder UsingStaticUser(string userId)
    {
        Builder.Services.AddScoped<AuditInterceptor>(provider =>
        {
            var metrics = provider.GetService<FSEntityFrameworkMetrics>();
            return new AuditInterceptor(() => userId, metrics: metrics);
        });

        return Builder;
    }
}