using System.Reflection;
using FS.EntityFramework.Library.Attributes;
using FS.EntityFramework.Library.Events;
using FS.EntityFramework.Library.Extensions;
using FS.EntityFramework.Library.Interceptors;
using Microsoft.Extensions.DependencyInjection;

namespace FS.EntityFramework.Library.FluentConfiguration;

/// <summary>
/// Internal implementation of the domain events configuration builder
/// </summary>
internal class DomainEventsConfigurationBuilder : IDomainEventsConfigurationBuilder
{
    private bool _dispatcherConfigured = false;

    /// <summary>
    /// Initializes a new instance of the DomainEventsConfigurationBuilder class
    /// </summary>
    /// <param name="builder">The parent FS.EntityFramework builder</param>
    public DomainEventsConfigurationBuilder(IFSEntityFrameworkBuilder builder)
    {
        Builder = builder;
    }

    /// <summary>
    /// Gets the parent FS.EntityFramework builder
    /// </summary>
    public IFSEntityFrameworkBuilder Builder { get; }

    /// <summary>
    /// Uses the default domain event dispatcher
    /// </summary>
    /// <returns>The domain events configuration builder for further configuration</returns>
    public IDomainEventsConfigurationBuilder UsingDefaultDispatcher()
    {
        if (_dispatcherConfigured)
            throw new InvalidOperationException("Domain event dispatcher has already been configured.");

        Builder.Services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        Builder.Services.AddScoped<DomainEventInterceptor>();
        _dispatcherConfigured = true;
        
        return this;
    }

    /// <summary>
    /// Uses a custom domain event dispatcher
    /// </summary>
    /// <typeparam name="TDispatcher">The custom dispatcher implementation</typeparam>
    /// <returns>The domain events configuration builder for further configuration</returns>
    public IDomainEventsConfigurationBuilder UsingCustomDispatcher<TDispatcher>()
        where TDispatcher : class, IDomainEventDispatcher
    {
        if (_dispatcherConfigured)
            throw new InvalidOperationException("Domain event dispatcher has already been configured.");

        Builder.Services.AddScoped<IDomainEventDispatcher, TDispatcher>();
        Builder.Services.AddScoped<DomainEventInterceptor>();
        _dispatcherConfigured = true;
        
        return this;
    }

    /// <summary>
    /// Automatically registers domain event handlers from the calling assembly
    /// </summary>
    /// <returns>The domain events configuration builder for further configuration</returns>
    public IDomainEventsConfigurationBuilder WithAutoHandlerDiscovery()
    {
        EnsureDispatcherConfigured();
        Builder.Services.AddDomainEventHandlersFromCallingAssembly();
        return this;
    }

    /// <summary>
    /// Automatically registers domain event handlers from the specified assembly
    /// </summary>
    /// <param name="assembly">The assembly to scan for handlers</param>
    /// <returns>The domain events configuration builder for further configuration</returns>
    public IDomainEventsConfigurationBuilder WithAutoHandlerDiscovery(Assembly assembly)
    {
        EnsureDispatcherConfigured();
        Builder.Services.AddDomainEventHandlersFromAssembly(assembly);
        return this;
    }

    /// <summary>
    /// Automatically registers domain event handlers from multiple assemblies
    /// </summary>
    /// <param name="assemblies">The assemblies to scan for handlers</param>
    /// <returns>The domain events configuration builder for further configuration</returns>
    public IDomainEventsConfigurationBuilder WithAutoHandlerDiscovery(params Assembly[] assemblies)
    {
        EnsureDispatcherConfigured();
        Builder.Services.AddDomainEventHandlersFromAssemblies(assemblies);
        return this;
    }

    /// <summary>
    /// Registers domain event handlers from assemblies containing the specified types
    /// </summary>
    /// <param name="types">Types whose assemblies will be scanned</param>
    /// <returns>The domain events configuration builder for further configuration</returns>
    public IDomainEventsConfigurationBuilder WithAutoHandlerDiscoveryFromTypes(params Type[] types)
    {
        EnsureDispatcherConfigured();
        Builder.Services.AddDomainEventHandlersFromAssemblyContaining(types);
        return this;
    }

    /// <summary>
    /// Registers domain event handlers using attribute-based discovery
    /// </summary>
    /// <param name="assembly">The assembly to scan for attributed handlers</param>
    /// <returns>The domain events configuration builder for further configuration</returns>
    public IDomainEventsConfigurationBuilder WithAttributeBasedDiscovery(Assembly assembly)
    {
        EnsureDispatcherConfigured();
        Builder.Services.AddAttributedDomainEventHandlers(assembly);
        return this;
    }

    /// <summary>
    /// Registers domain event handlers with custom filtering
    /// </summary>
    /// <param name="assembly">The assembly to scan</param>
    /// <param name="filter">Custom filter for handler types</param>
    /// <param name="serviceLifetime">Service lifetime for registered handlers</param>
    /// <returns>The domain events configuration builder for further configuration</returns>
    public IDomainEventsConfigurationBuilder WithCustomHandlerDiscovery(
        Assembly assembly,
        Func<Type, bool>? filter = null,
        ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
    {
        EnsureDispatcherConfigured();
        Builder.Services.AddDomainEventHandlers(assembly, filter, serviceLifetime);
        return this;
    }

    /// <summary>
    /// Manually registers a specific domain event handler
    /// </summary>
    /// <typeparam name="TEvent">The domain event type</typeparam>
    /// <typeparam name="THandler">The handler implementation</typeparam>
    /// <returns>The domain events configuration builder for further configuration</returns>
    public IDomainEventsConfigurationBuilder WithHandler<TEvent, THandler>()
        where TEvent : class, FS.EntityFramework.Library.Common.IDomainEvent
        where THandler : class, IDomainEventHandler<TEvent>
    {
        EnsureDispatcherConfigured();
        Builder.Services.AddDomainEventHandler<TEvent, THandler>();
        return this;
    }

    /// <summary>
    /// Completes the domain events configuration and returns to the main builder
    /// </summary>
    /// <returns>The parent builder for method chaining</returns>
    public IFSEntityFrameworkBuilder Complete()
    {
        EnsureDispatcherConfigured();
        return Builder;
    }

    /// <summary>
    /// Ensures that a domain event dispatcher has been configured
    /// </summary>
    private void EnsureDispatcherConfigured()
    {
        if (!_dispatcherConfigured)
        {
            UsingDefaultDispatcher();
        }
    }
}