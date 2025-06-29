using System.Reflection;
using FS.EntityFramework.Library.Events;
using Microsoft.Extensions.DependencyInjection;

namespace FS.EntityFramework.Library.FluentConfiguration;

/// <summary>
/// Fluent configuration extensions for domain events functionality
/// </summary>
public static class DomainEventsConfigurationExtensions
{
    /// <summary>
    /// Configures domain events support
    /// </summary>
    /// <param name="builder">The FS.EntityFramework builder</param>
    /// <returns>The domain events configuration builder for further configuration</returns>
    public static IDomainEventsConfigurationBuilder WithDomainEvents(this IFSEntityFrameworkBuilder builder)
    {
        return new DomainEventsConfigurationBuilder(builder);
    }
}

/// <summary>
/// Interface for configuring domain events functionality
/// </summary>
public interface IDomainEventsConfigurationBuilder
{
    /// <summary>
    /// Gets the parent FS.EntityFramework builder
    /// </summary>
    IFSEntityFrameworkBuilder Builder { get; }
    
    /// <summary>
    /// Uses the default domain event dispatcher
    /// </summary>
    /// <returns>The domain events configuration builder for further configuration</returns>
    IDomainEventsConfigurationBuilder UsingDefaultDispatcher();
    
    /// <summary>
    /// Uses a custom domain event dispatcher
    /// </summary>
    /// <typeparam name="TDispatcher">The custom dispatcher implementation</typeparam>
    /// <returns>The domain events configuration builder for further configuration</returns>
    IDomainEventsConfigurationBuilder UsingCustomDispatcher<TDispatcher>()
        where TDispatcher : class, IDomainEventDispatcher;
    
    /// <summary>
    /// Automatically registers domain event handlers from the calling assembly
    /// </summary>
    /// <returns>The domain events configuration builder for further configuration</returns>
    IDomainEventsConfigurationBuilder WithAutoHandlerDiscovery();
    
    /// <summary>
    /// Automatically registers domain event handlers from the specified assembly
    /// </summary>
    /// <param name="assembly">The assembly to scan for handlers</param>
    /// <returns>The domain events configuration builder for further configuration</returns>
    IDomainEventsConfigurationBuilder WithAutoHandlerDiscovery(Assembly assembly);
    
    /// <summary>
    /// Automatically registers domain event handlers from multiple assemblies
    /// </summary>
    /// <param name="assemblies">The assemblies to scan for handlers</param>
    /// <returns>The domain events configuration builder for further configuration</returns>
    IDomainEventsConfigurationBuilder WithAutoHandlerDiscovery(params Assembly[] assemblies);
    
    /// <summary>
    /// Registers domain event handlers from assemblies containing the specified types
    /// </summary>
    /// <param name="types">Types whose assemblies will be scanned</param>
    /// <returns>The domain events configuration builder for further configuration</returns>
    IDomainEventsConfigurationBuilder WithAutoHandlerDiscoveryFromTypes(params Type[] types);
    
    /// <summary>
    /// Registers domain event handlers using attribute-based discovery
    /// </summary>
    /// <param name="assembly">The assembly to scan for attributed handlers</param>
    /// <returns>The domain events configuration builder for further configuration</returns>
    IDomainEventsConfigurationBuilder WithAttributeBasedDiscovery(Assembly assembly);
    
    /// <summary>
    /// Registers domain event handlers with custom filtering
    /// </summary>
    /// <param name="assembly">The assembly to scan</param>
    /// <param name="filter">Custom filter for handler types</param>
    /// <param name="serviceLifetime">Service lifetime for registered handlers</param>
    /// <returns>The domain events configuration builder for further configuration</returns>
    IDomainEventsConfigurationBuilder WithCustomHandlerDiscovery(
        Assembly assembly,
        Func<Type, bool>? filter = null,
        ServiceLifetime serviceLifetime = ServiceLifetime.Scoped);
    
    /// <summary>
    /// Manually registers a specific domain event handler
    /// </summary>
    /// <typeparam name="TEvent">The domain event type</typeparam>
    /// <typeparam name="THandler">The handler implementation</typeparam>
    /// <returns>The domain events configuration builder for further configuration</returns>
    IDomainEventsConfigurationBuilder WithHandler<TEvent, THandler>()
        where TEvent : class, FS.EntityFramework.Library.Common.IDomainEvent
        where THandler : class, IDomainEventHandler<TEvent>;
    
    /// <summary>
    /// Completes the domain events configuration and returns to the main builder
    /// </summary>
    /// <returns>The parent builder for method chaining</returns>
    IFSEntityFrameworkBuilder Complete();
}