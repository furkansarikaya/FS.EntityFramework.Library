using Microsoft.Extensions.DependencyInjection;

namespace FS.EntityFramework.Library.FluentConfiguration;

/// <summary>
/// Fluent configuration builder interface for FS.EntityFramework.Library services
/// </summary>
public interface IFSEntityFrameworkBuilder
{
    /// <summary>
    /// Gets the service collection being configured
    /// </summary>
    IServiceCollection Services { get; }
    
    /// <summary>
    /// Gets the DbContext type being configured
    /// </summary>
    Type DbContextType { get; }
}
