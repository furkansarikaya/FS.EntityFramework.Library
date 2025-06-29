using Microsoft.Extensions.DependencyInjection;

namespace FS.EntityFramework.Library.FluentConfiguration;

/// <summary>
/// Internal implementation of the fluent configuration builder for FS.EntityFramework.Library
/// </summary>
internal class FSEntityFrameworkBuilder : IFSEntityFrameworkBuilder
{
    /// <summary>
    /// Initializes a new instance of the FSEntityFrameworkBuilder class
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="dbContextType">The DbContext type</param>
    public FSEntityFrameworkBuilder(IServiceCollection services, Type dbContextType)
    {
        Services = services;
        DbContextType = dbContextType;
    }

    /// <summary>
    /// Gets the service collection being configured
    /// </summary>
    public IServiceCollection Services { get; }
    
    /// <summary>
    /// Gets the DbContext type being configured
    /// </summary>
    public Type DbContextType { get; }
}