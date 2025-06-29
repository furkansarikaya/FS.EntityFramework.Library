using System.Reflection;
using FS.EntityFramework.Library.Events;
using Microsoft.Extensions.DependencyInjection;

namespace FS.EntityFramework.Library.Attributes;

// Extensions with Attribute Support
/// <summary>
/// Extension methods for configuring domain events
/// </summary>
public static partial class DomainEventExtensions
{
    /// <summary>
    /// Scans and registers domain event handlers marked with DomainEventHandlerAttribute
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="assembly">The assembly to scan</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddAttributedDomainEventHandlers(
        this IServiceCollection services,
        Assembly assembly)
    {
        var handlerTypes = assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .Where(type => type.GetCustomAttribute<DomainEventHandlerAttribute>() != null)
            .Where(type => type.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDomainEventHandler<>)))
            .ToList();

        foreach (var handlerType in handlerTypes)
        {
            var attribute = handlerType.GetCustomAttribute<DomainEventHandlerAttribute>()!;
            
            if (!attribute.IsEnabled)
                continue;

            var interfaceTypes = handlerType.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDomainEventHandler<>));

            foreach (var interfaceType in interfaceTypes)
            {
                services.Add(new ServiceDescriptor(interfaceType, handlerType, attribute.ServiceLifetime));
            }
        }

        return services;
    }
}