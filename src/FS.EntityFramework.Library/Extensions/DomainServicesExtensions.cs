using FS.EntityFramework.Library.Common;
using Microsoft.Extensions.DependencyInjection;

namespace FS.EntityFramework.Library.Extensions;

/// <summary>
/// Extension methods for configuring Domain-Driven Design services
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
    /// Automatically registers domain repositories for all aggregate roots in the specified assembly
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
            .Where(type => IsAggregateRoot(type))
            .ToList();

        foreach (var aggregateType in aggregateTypes)
        {
            var keyType = GetAggregateKeyType(aggregateType);
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
    /// Determines if a type is an aggregate root
    /// </summary>
    /// <param name="type">The type to check</param>
    /// <returns>True if the type is an aggregate root; otherwise false</returns>
    private static bool IsAggregateRoot(Type type)
    {
        var current = type;
        while (current != null)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(AggregateRoot<>))
            {
                return true;
            }

            current = current.BaseType;
        }

        return false;
    }

    /// <summary>
    /// Gets the key type of an aggregate root
    /// </summary>
    /// <param name="aggregateType">The aggregate type</param>
    /// <returns>The key type if found; otherwise null</returns>
    private static Type? GetAggregateKeyType(Type aggregateType)
    {
        var current = aggregateType;
        while (current != null)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(AggregateRoot<>))
            {
                return current.GetGenericArguments()[0];
            }

            current = current.BaseType;
        }

        return null;
    }
}