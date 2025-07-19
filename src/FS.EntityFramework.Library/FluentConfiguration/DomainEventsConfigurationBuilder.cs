using System.Reflection;
using FS.EntityFramework.Library.Attributes;
using FS.EntityFramework.Library.Events;
using FS.EntityFramework.Library.Extensions;
using FS.EntityFramework.Library.Interceptors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FS.EntityFramework.Library.FluentConfiguration;

/// <summary>
/// Enhanced implementation of the domain events configuration builder with better user guidance
/// Now provides clear feedback about configuration choices and prevents silent fallbacks
/// </summary>
internal class DomainEventsConfigurationBuilder : IDomainEventsConfigurationBuilder
{
    private bool _dispatcherConfigured = false;
    private bool _explicitDispatcherChoice = false; // Track if user made explicit choice
    
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
    /// Uses the default domain event dispatcher with clear user notification
    /// </summary>
    /// <returns>The domain events configuration builder for further configuration</returns>
    public IDomainEventsConfigurationBuilder UsingDefaultDispatcher()
    {
        ThrowIfDispatcherAlreadyConfigured();

        Builder.Services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        Builder.Services.AddScoped<DomainEventInterceptor>();
        
        _dispatcherConfigured = true;
        _explicitDispatcherChoice = true; // User explicitly chose default
        
        // Add configuration metadata for logging and debugging
        Builder.Services.AddSingleton<IDomainEventsConfigurationInfo>(
            new DomainEventsConfigurationInfo("DefaultDispatcher", true));
        
        return this;
    }

    /// <summary>
    /// Uses a custom domain event dispatcher with validation
    /// </summary>
    /// <typeparam name="TDispatcher">The custom dispatcher implementation</typeparam>
    /// <returns>The domain events configuration builder for further configuration</returns>
    public IDomainEventsConfigurationBuilder UsingCustomDispatcher<TDispatcher>()
        where TDispatcher : class, IDomainEventDispatcher
    {
        ThrowIfDispatcherAlreadyConfigured();

        Builder.Services.AddScoped<IDomainEventDispatcher, TDispatcher>();
        Builder.Services.AddScoped<DomainEventInterceptor>();
        
        _dispatcherConfigured = true;
        _explicitDispatcherChoice = true; // User explicitly chose custom
        
        // Add configuration metadata
        Builder.Services.AddSingleton<IDomainEventsConfigurationInfo>(
            new DomainEventsConfigurationInfo(typeof(TDispatcher).Name, true));
        
        return this;
    }

    /// <summary>
    /// Automatically registers domain event handlers from the calling assembly
    /// </summary>
    /// <returns>The domain events configuration builder for further configuration</returns>
    public IDomainEventsConfigurationBuilder WithAutoHandlerDiscovery()
    {
        EnsureDispatcherConfiguredWithWarning();
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
        EnsureDispatcherConfiguredWithWarning();
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
        EnsureDispatcherConfiguredWithWarning();
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
        EnsureDispatcherConfiguredWithWarning();
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
        EnsureDispatcherConfiguredWithWarning();
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
        EnsureDispatcherConfiguredWithWarning();
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
        EnsureDispatcherConfiguredWithWarning();
        Builder.Services.AddDomainEventHandler<TEvent, THandler>();
        return this;
    }

    /// <summary>
    /// ENHANCED: Completes the domain events configuration with proper validation
    /// Now throws meaningful exceptions if configuration is incomplete
    /// </summary>
    /// <returns>The parent builder for method chaining</returns>
    public IFSEntityFrameworkBuilder Complete()
    {
        // CRITICAL ENHANCEMENT: Validate configuration before completing
        if (!_dispatcherConfigured)
        {
            throw new InvalidOperationException(
                "Domain events configuration is incomplete. " +
                "You must choose a dispatcher by calling either UsingDefaultDispatcher() or UsingCustomDispatcher<T>(). " +
                "Example: .WithDomainEvents().UsingDefaultDispatcher().Complete()");
        }

        // Provide helpful information about the configuration
        var serviceProvider = Builder.Services.BuildServiceProvider();
        var logger = serviceProvider.CreateScope().ServiceProvider.GetService<ILogger<DomainEventsConfigurationBuilder>>();

        if (logger == null || !_explicitDispatcherChoice) return Builder;
        var configInfo = serviceProvider.GetService<IDomainEventsConfigurationInfo>();
        logger.LogInformation("Domain events configured with dispatcher: {DispatcherType}", 
            configInfo?.DispatcherType ?? "Unknown");

        return Builder;
    }

    /// <summary>
    /// ENHANCED: Ensures dispatcher is configured with clear user guidance
    /// No more silent fallbacks - users must make explicit choices
    /// </summary>
    private void EnsureDispatcherConfiguredWithWarning()
    {
        if (!_dispatcherConfigured)
        {
            // Instead of silently using default, throw a helpful exception
            throw new InvalidOperationException(
                "Domain event dispatcher has not been configured. " +
                "Please call UsingDefaultDispatcher() or UsingCustomDispatcher<T>() first. " +
                "Example: .WithDomainEvents().UsingDefaultDispatcher().WithAutoHandlerDiscovery()");
        }
    }
    
    /// <summary>
    /// Helper method to check for dispatcher conflicts
    /// </summary>
    private void ThrowIfDispatcherAlreadyConfigured()
    {
        if (_dispatcherConfigured)
        {
            throw new InvalidOperationException(
                "Domain event dispatcher has already been configured. " +
                "You can only configure one dispatcher per domain events setup. " +
                "If you need to change the dispatcher, create a new configuration.");
        }
    }
}

/// <summary>
/// Configuration information for domain events setup
/// Helps with debugging and provides transparency about configuration choices
/// </summary>
public interface IDomainEventsConfigurationInfo
{
    string DispatcherType { get; }
    bool IsExplicitChoice { get; }
    DateTime ConfiguredAt { get; }
}

/// <summary>
/// Implementation of domain events configuration information
/// </summary>
internal class DomainEventsConfigurationInfo(string dispatcherType, bool isExplicitChoice) : IDomainEventsConfigurationInfo
{
    public string DispatcherType { get; } = dispatcherType;
    public bool IsExplicitChoice { get; } = isExplicitChoice;
    public DateTime ConfiguredAt { get; } = DateTime.UtcNow;
}