using System.Reflection;
using FS.EntityFramework.Library.Common;
using FS.EntityFramework.Library.Events;
using FS.EntityFramework.Library.Interceptors;
using Microsoft.Extensions.DependencyInjection;

namespace FS.EntityFramework.Library.Extensions;

/// <summary>
/// Extension methods for configuring domain events
/// </summary>
public static class DomainEventExtensions
{
    /// <summary>
    /// Adds domain event support to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddDomainEvents(this IServiceCollection services)
    {
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddScoped<DomainEventInterceptor>();
        return services;
    }

    /// <summary>
    /// Adds domain event support with custom dispatcher
    /// </summary>
    /// <typeparam name="TDispatcher">Custom dispatcher implementation</typeparam>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddDomainEvents<TDispatcher>(this IServiceCollection services)
        where TDispatcher : class, IDomainEventDispatcher
    {
        services.AddScoped<IDomainEventDispatcher, TDispatcher>();
        services.AddScoped<DomainEventInterceptor>();
        return services;
    }

    /// <summary>
    /// Adds a single domain event handler to the service collection
    /// </summary>
    /// <typeparam name="TEvent">The domain event type</typeparam>
    /// <typeparam name="THandler">The handler implementation</typeparam>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddDomainEventHandler<TEvent, THandler>(this IServiceCollection services)
        where TEvent : class, IDomainEvent
        where THandler : class, IDomainEventHandler<TEvent>
    {
        services.AddScoped<IDomainEventHandler<TEvent>, THandler>();
        return services;
    }

    /// <summary>
    /// Automatically scans and registers all domain event handlers from the specified assembly
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="assembly">The assembly to scan for handlers</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddDomainEventHandlersFromAssembly(
        this IServiceCollection services, 
        Assembly assembly)
    {
        var handlerTypes = assembly.GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract)
            .Where(type => type.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDomainEventHandler<>)))
            .ToList();

        foreach (var handlerType in handlerTypes)
        {
            var interfaceTypes = handlerType.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDomainEventHandler<>));

            foreach (var interfaceType in interfaceTypes)
            {
                services.AddScoped(interfaceType, handlerType);
            }
        }

        return services;
    }

    /// <summary>
    /// Automatically scans and registers all domain event handlers from the calling assembly
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddDomainEventHandlersFromCallingAssembly(this IServiceCollection services)
    {
        var callingAssembly = Assembly.GetCallingAssembly();
        return services.AddDomainEventHandlersFromAssembly(callingAssembly);
    }

    /// <summary>
    /// Automatically scans and registers all domain event handlers from the specified assemblies
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="assemblies">The assemblies to scan for handlers</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddDomainEventHandlersFromAssemblies(
        this IServiceCollection services, 
        params Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            services.AddDomainEventHandlersFromAssembly(assembly);
        }
        return services;
    }

    /// <summary>
    /// Automatically scans and registers all domain event handlers from assemblies containing the specified types
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddDomainEventHandlersFromAssemblyContaining<T>(this IServiceCollection services)
    {
        return services.AddDomainEventHandlersFromAssembly(typeof(T).Assembly);
    }

    /// <summary>
    /// Automatically scans and registers all domain event handlers from assemblies containing the specified types
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="types">Types whose assemblies will be scanned</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddDomainEventHandlersFromAssemblyContaining(
        this IServiceCollection services, 
        params Type[] types)
    {
        var assemblies = types.Select(t => t.Assembly).Distinct();
        return services.AddDomainEventHandlersFromAssemblies(assemblies.ToArray());
    }

    /// <summary>
    /// Scans and registers domain event handlers with custom filtering
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="assembly">The assembly to scan</param>
    /// <param name="filter">Custom filter for handler types</param>
    /// <param name="serviceLifetime">Service lifetime for registered handlers</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddDomainEventHandlers(
        this IServiceCollection services,
        Assembly assembly,
        Func<Type, bool>? filter = null,
        ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
    {
        var handlerTypes = assembly.GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract)
            .Where(type => type.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDomainEventHandler<>)))
            .Where(filter ?? (_ => true))
            .ToList();

        foreach (var handlerType in handlerTypes)
        {
            var interfaceTypes = handlerType.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDomainEventHandler<>));

            foreach (var interfaceType in interfaceTypes)
            {
                services.Add(new ServiceDescriptor(interfaceType, handlerType, serviceLifetime));
            }
        }

        return services;
    }

    /// <summary>
    /// Registers domain event handlers with decorator pattern support
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="assembly">The assembly to scan</param>
    /// <param name="decoratorType">Decorator type to wrap handlers</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddDomainEventHandlersWithDecorator(
        this IServiceCollection services,
        Assembly assembly,
        Type? decoratorType = null)
    {
        var handlerTypes = assembly.GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract)
            .Where(type => type.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDomainEventHandler<>)))
            .ToList();

        foreach (var handlerType in handlerTypes)
        {
            var interfaceTypes = handlerType.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDomainEventHandler<>));

            foreach (var interfaceType in interfaceTypes)
            {
                if (decoratorType != null)
                {
                    // Register original handler
                    services.AddScoped(handlerType);
                    
                    // Register decorated handler
                    services.AddScoped(interfaceType, provider =>
                    {
                        var handler = provider.GetRequiredService(handlerType);
                        return Activator.CreateInstance(decoratorType, handler)!;
                    });
                }
                else
                {
                    services.AddScoped(interfaceType, handlerType);
                }
            }
        }

        return services;
    }
}