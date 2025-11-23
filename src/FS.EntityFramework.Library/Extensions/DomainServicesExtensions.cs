using FS.EntityFramework.Library.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FS.EntityFramework.Library.Extensions;

/// <summary>
/// Extension methods for configuring Domain-Driven Design services
/// Enhanced with robust inheritance detection and DbContext support
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
            serviceProvider =>
            {
                var unitOfWork = serviceProvider.GetRequiredService<UnitOfWorks.IUnitOfWork>();
                var context = serviceProvider.GetRequiredService<DbContext>();
                return new Infrastructure.DomainRepository<TAggregate, TKey>(unitOfWork, context);
            },
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

            services.Add(new ServiceDescriptor(
                repositoryInterfaceType,
                serviceProvider =>
                {
                    var unitOfWork = serviceProvider.GetRequiredService<UnitOfWorks.IUnitOfWork>();
                    var context = serviceProvider.GetRequiredService<DbContext>();
                    return Activator.CreateInstance(repositoryImplementationType, unitOfWork, context)!;
                },
                serviceLifetime));
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
    /// </summary>
    private static bool IsAggregateRootEnhanced(Type type)
    {
        var current = type;
        
        while (current != null)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(AggregateRoot<>))
            {
                return true;
            }
            
            if (current == typeof(AggregateRoot))
            {
                return true;
            }
            
            if (InheritsFromAggregateRoot(current))
            {
                return true;
            }
            
            current = current.BaseType;
        }

        return false;
    }
    
    /// <summary>
    /// Helper method to check if a type inherits from any AggregateRoot variant
    /// </summary>
    private static bool InheritsFromAggregateRoot(Type type)
    {
        var allTypes = new List<Type>();
        
        var current = type.BaseType;
        while (current != null)
        {
            allTypes.Add(current);
            current = current.BaseType;
        }
        
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
    /// </summary>
    private static Type? GetAggregateKeyTypeEnhanced(Type aggregateType)
    {
        var current = aggregateType;
        
        while (current != null)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(AggregateRoot<>))
            {
                return current.GetGenericArguments()[0];
            }
            
            if (current == typeof(AggregateRoot))
            {
                return typeof(Guid);
            }
            
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