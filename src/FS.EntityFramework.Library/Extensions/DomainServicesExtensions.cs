using FS.EntityFramework.Library.Common;
using Microsoft.Extensions.DependencyInjection;

namespace FS.EntityFramework.Library.Extensions;

/// <summary>
/// Extension methods for configuring Domain-Driven Design services
/// Enhanced with robust inheritance detection
/// </summary>
public static class DomainServicesExtensions
{
    /// <summary>
    /// Adds Domain-Driven Design services to the service collection
    /// Registers domain repositories, unit of work, and related services
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddDomainServices(this IServiceCollection services)
    {
        // Register domain unit of work
        services.AddScoped<Domain.IDomainUnitOfWork, Infrastructure.DomainUnitOfWork>();

        return services;
    }

    /// <summary>
    /// Registers a domain repository for a specific aggregate root
    /// </summary>
    /// <typeparam name="TAggregate">The aggregate root type</typeparam>
    /// <typeparam name="TKey">The aggregate identifier type</typeparam>
    /// <param name="services">The service collection</param>
    /// <param name="serviceLifetime">The service lifetime (default: Scoped)</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddDomainRepository<TAggregate, TKey>(
        this IServiceCollection services,
        ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
        where TAggregate : AggregateRoot<TKey>
        where TKey : IEquatable<TKey>
    {
        services.Add(new ServiceDescriptor(
            typeof(Domain.IDomainRepository<TAggregate, TKey>),
            typeof(Infrastructure.DomainRepository<TAggregate, TKey>),
            serviceLifetime));

        return services;
    }

    /// <summary>
    /// Registers a custom domain repository implementation
    /// </summary>
    /// <typeparam name="TAggregate">The aggregate root type</typeparam>
    /// <typeparam name="TKey">The aggregate identifier type</typeparam>
    /// <typeparam name="TRepository">The custom repository implementation</typeparam>
    /// <param name="services">The service collection</param>
    /// <param name="serviceLifetime">The service lifetime (default: Scoped)</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddCustomDomainRepository<TAggregate, TKey, TRepository>(
        this IServiceCollection services,
        ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
        where TAggregate : AggregateRoot<TKey>
        where TKey : IEquatable<TKey>
        where TRepository : class, Domain.IDomainRepository<TAggregate, TKey>
    {
        services.Add(new ServiceDescriptor(
            typeof(Domain.IDomainRepository<TAggregate, TKey>),
            typeof(TRepository),
            serviceLifetime));

        return services;
    }

    /// <summary>
    /// ENHANCED: Automatically registers domain repositories for all aggregate roots in the specified assembly
    /// Now with robust inheritance detection that handles complex inheritance hierarchies
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="assembly">The assembly to scan for aggregate roots</param>
    /// <param name="serviceLifetime">The service lifetime (default: Scoped)</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddDomainRepositoriesFromAssembly(
        this IServiceCollection services,
        System.Reflection.Assembly assembly,
        ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
    {
        var aggregateTypes = assembly.GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract)
            .Where(type => IsAggregateRootEnhanced(type))
            .ToList();

        foreach (var aggregateType in aggregateTypes)
        {
            var keyType = GetAggregateKeyTypeEnhanced(aggregateType);
            if (keyType == null) continue;

            var repositoryInterfaceType = typeof(Domain.IDomainRepository<,>).MakeGenericType(aggregateType, keyType);
            var repositoryImplementationType = typeof(Infrastructure.DomainRepository<,>).MakeGenericType(aggregateType, keyType);

            services.Add(new ServiceDescriptor(repositoryInterfaceType, repositoryImplementationType, serviceLifetime));
        }

        return services;
    }

    /// <summary>
    /// Registers domain repositories for aggregate roots in the calling assembly
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="serviceLifetime">The service lifetime (default: Scoped)</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddDomainRepositoriesFromCallingAssembly(
        this IServiceCollection services,
        ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
    {
        var callingAssembly = System.Reflection.Assembly.GetCallingAssembly();
        return services.AddDomainRepositoriesFromAssembly(callingAssembly, serviceLifetime);
    }

    /// <summary>
    /// Adds enhanced domain validation services
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddDomainValidation(this IServiceCollection services)
    {
        // Domain validation services can be added here
        // For example: custom validation services, rule engines, etc.
        return services;
    }

    /// <summary>
    /// ENHANCED: Determines if a type is an aggregate root with robust inheritance detection
    /// This method now properly handles complex inheritance hierarchies including:
    /// - Direct inheritance: MyAggregate : AggregateRoot<Guid>
    /// - Intermediate inheritance: MyAggregate : BaseAggregate : AggregateRoot<Guid>
    /// - Multiple levels: SpecificAggregate : IntermediateAggregate : BaseAggregate : AggregateRoot<Guid>
    /// </summary>
    /// <param name="type">The type to check</param>
    /// <returns>True if the type is or inherits from an aggregate root; otherwise false</returns>
    private static bool IsAggregateRootEnhanced(Type type)
    {
        // Start with the type itself and walk up the inheritance chain
        var current = type;
        
        while (current != null)
        {
            // Check if current type directly inherits from AggregateRoot<T>
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(AggregateRoot<>))
            {
                return true;
            }
            
            // ENHANCEMENT: Also check if current type inherits from the non-generic AggregateRoot
            // This handles cases where entities inherit from AggregateRoot (which inherits from AggregateRoot<Guid>)
            if (current == typeof(AggregateRoot))
            {
                return true;
            }
            
            // CRITICAL FIX: Check if any base class is a constructed generic type based on AggregateRoot<>
            // This handles inheritance chains like: SpecificProduct : BaseProduct : AggregateRoot<Guid>
            if (InheritsFromAggregateRoot(current))
            {
                return true;
            }
            
            // Move to the base type
            current = current.BaseType;
        }

        return false;
    }
    
    /// <summary>
    /// Helper method to check if a type inherits from any AggregateRoot variant
    /// This method performs deep inheritance analysis
    /// </summary>
    /// <param name="type">The type to check</param>
    /// <returns>True if the type inherits from AggregateRoot in any form</returns>
    private static bool InheritsFromAggregateRoot(Type type)
    {
        // Check all interfaces and base types
        var allTypes = new List<Type>();
        
        // Add base types
        var current = type.BaseType;
        while (current != null)
        {
            allTypes.Add(current);
            current = current.BaseType;
        }
        
        // Check each type in the hierarchy
        foreach (var checkType in allTypes)
        {
            if (checkType.IsGenericType && checkType.GetGenericTypeDefinition() == typeof(AggregateRoot<>))
            {
                return true;
            }
            
            if (checkType == typeof(AggregateRoot))
            {
                return true;
            }
        }
        
        return false;
    }

    /// <summary>
    /// ENHANCED: Gets the key type of an aggregate root with improved detection
    /// Now handles complex inheritance scenarios properly
    /// </summary>
    /// <param name="aggregateType">The aggregate type</param>
    /// <returns>The key type if found; otherwise null</returns>
    private static Type? GetAggregateKeyTypeEnhanced(Type aggregateType)
    {
        var current = aggregateType;
        
        while (current != null)
        {
            // Check if current type is directly AggregateRoot<TKey>
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(AggregateRoot<>))
            {
                return current.GetGenericArguments()[0];
            }
            
            // ENHANCEMENT: Check if current type inherits from non-generic AggregateRoot
            // Non-generic AggregateRoot inherits from AggregateRoot<Guid>, so key type is Guid
            if (current == typeof(AggregateRoot))
            {
                return typeof(Guid);
            }
            
            // ENHANCEMENT: Check base types for generic AggregateRoot
            var baseType = current.BaseType;
            if (baseType is { IsGenericType: true } && baseType.GetGenericTypeDefinition() == typeof(AggregateRoot<>))
            {
                return baseType.GetGenericArguments()[0];
            }
            
            current = current.BaseType;
        }

        return null;
    }
}