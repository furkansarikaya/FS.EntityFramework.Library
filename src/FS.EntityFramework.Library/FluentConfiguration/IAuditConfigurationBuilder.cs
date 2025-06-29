namespace FS.EntityFramework.Library.FluentConfiguration;

/// <summary>
/// Interface for configuring audit functionality
/// </summary>
public interface IAuditConfigurationBuilder
{
    /// <summary>
    /// Gets the parent FS.EntityFramework builder
    /// </summary>
    IFSEntityFrameworkBuilder Builder { get; }
    
    /// <summary>
    /// Configures the user provider using a delegate function
    /// </summary>
    /// <param name="getCurrentUser">Function to get the current user identifier</param>
    /// <param name="getCurrentTime">Optional function to get the current time (defaults to UTC now)</param>
    /// <returns>The parent builder for method chaining</returns>
    IFSEntityFrameworkBuilder UsingUserProvider(
        Func<IServiceProvider, string?> getCurrentUser,
        Func<IServiceProvider, DateTime>? getCurrentTime = null);
    
    /// <summary>
    /// Configures the user provider using an interface-based approach
    /// </summary>
    /// <typeparam name="TUserContext">The user context interface implementation</typeparam>
    /// <returns>The parent builder for method chaining</returns>
    IFSEntityFrameworkBuilder UsingUserContext<TUserContext>()
        where TUserContext : class, IUserContext;
    
    /// <summary>
    /// Configures the user provider using HttpContext for web applications
    /// </summary>
    /// <param name="claimType">The claim type to extract user ID from (defaults to NameIdentifier)</param>
    /// <returns>The parent builder for method chaining</returns>
    IFSEntityFrameworkBuilder UsingHttpContext(string claimType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
    
    /// <summary>
    /// Configures the user provider using a static user ID (typically for testing)
    /// </summary>
    /// <param name="userId">The static user ID to use</param>
    /// <returns>The parent builder for method chaining</returns>
    IFSEntityFrameworkBuilder UsingStaticUser(string userId);
}