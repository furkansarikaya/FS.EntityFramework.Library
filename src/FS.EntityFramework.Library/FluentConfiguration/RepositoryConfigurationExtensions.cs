using FS.EntityFramework.Library.Common;
using FS.EntityFramework.Library.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace FS.EntityFramework.Library.FluentConfiguration;

/// <summary>
/// Fluent configuration extensions for repository registration
/// </summary>
public static class RepositoryConfigurationExtensions
{
    /// <summary>
    /// Registers a custom repository implementation for a specific entity
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <typeparam name="TKey">The entity's primary key type</typeparam>
    /// <typeparam name="TRepository">The custom repository implementation</typeparam>
    /// <param name="builder">The FS.EntityFramework builder</param>
    /// <param name="serviceLifetime">The service lifetime for the repository</param>
    /// <returns>The builder for method chaining</returns>
    public static IFSEntityFrameworkBuilder WithCustomRepository<TEntity, TKey, TRepository>(
        this IFSEntityFrameworkBuilder builder,
        ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
        where TEntity : BaseEntity<TKey>
        where TKey : IEquatable<TKey>
        where TRepository : class, IRepository<TEntity, TKey>
    {
        builder.Services.Add(new ServiceDescriptor(
            typeof(IRepository<TEntity, TKey>),
            typeof(TRepository),
            serviceLifetime));

        return builder;
    }

    /// <summary>
    /// Registers multiple custom repositories from an assembly
    /// </summary>
    /// <param name="builder">The FS.EntityFramework builder</param>
    /// <param name="assembly">The assembly to scan for repositories</param>
    /// <param name="serviceLifetime">The service lifetime for the repositories</param>
    /// <returns>The builder for method chaining</returns>
    public static IFSEntityFrameworkBuilder WithRepositoriesFromAssembly(
        this IFSEntityFrameworkBuilder builder,
        System.Reflection.Assembly assembly,
        ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
    {
        var repositoryTypes = assembly.GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract)
            .Where(type => type.GetInterfaces()
                .Any(i => i.IsGenericType && 
                         i.GetGenericTypeDefinition() == typeof(IRepository<,>)))
            .ToList();

        foreach (var repositoryType in repositoryTypes)
        {
            var interfaceType = repositoryType.GetInterfaces()
                .First(i => i.IsGenericType && 
                           i.GetGenericTypeDefinition() == typeof(IRepository<,>));

            builder.Services.Add(new ServiceDescriptor(interfaceType, repositoryType, serviceLifetime));
        }

        return builder;
    }

    /// <summary>
    /// Registers repositories with a custom filter
    /// </summary>
    /// <param name="builder">The FS.EntityFramework builder</param>
    /// <param name="assembly">The assembly to scan</param>
    /// <param name="filter">Custom filter for repository types</param>
    /// <param name="serviceLifetime">The service lifetime for the repositories</param>
    /// <returns>The builder for method chaining</returns>
    public static IFSEntityFrameworkBuilder WithRepositories(
        this IFSEntityFrameworkBuilder builder,
        System.Reflection.Assembly assembly,
        Func<Type, bool>? filter = null,
        ServiceLifetime serviceLifetime = ServiceLifetime.Scoped)
    {
        var repositoryTypes = assembly.GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract)
            .Where(type => type.GetInterfaces()
                .Any(i => i.IsGenericType && 
                         i.GetGenericTypeDefinition() == typeof(IRepository<,>)))
            .Where(filter ?? (_ => true))
            .ToList();

        foreach (var repositoryType in repositoryTypes)
        {
            var interfaceTypes = repositoryType.GetInterfaces()
                .Where(i => i.IsGenericType && 
                           i.GetGenericTypeDefinition() == typeof(IRepository<,>));

            foreach (var interfaceType in interfaceTypes)
            {
                builder.Services.Add(new ServiceDescriptor(interfaceType, repositoryType, serviceLifetime));
            }
        }

        return builder;
    }
}