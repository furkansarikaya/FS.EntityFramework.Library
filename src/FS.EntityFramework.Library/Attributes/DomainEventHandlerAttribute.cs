using Microsoft.Extensions.DependencyInjection;

namespace FS.EntityFramework.Library.Attributes;

/// <summary>
/// Attribute to mark domain event handlers for automatic registration
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class DomainEventHandlerAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the service lifetime for this handler
    /// </summary>
    public ServiceLifetime ServiceLifetime { get; set; } = ServiceLifetime.Scoped;

    /// <summary>
    /// Gets or sets whether this handler should be registered
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the order of execution for this handler
    /// </summary>
    public int Order { get; set; } = 0;
}