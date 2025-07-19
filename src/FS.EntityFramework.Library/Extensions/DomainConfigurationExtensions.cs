using FS.EntityFramework.Library.Common;
using FS.EntityFramework.Library.FluentConfiguration;

namespace FS.EntityFramework.Library.Extensions;

/// <summary>
/// Fluent configuration extensions for Domain-Driven Design
/// </summary>
public static class DomainConfigurationExtensions
{
    /// <summary>
    /// Configures Domain-Driven Design services with fluent API
    /// </summary>
    /// <param name="builder">The FS.EntityFramework builder</param>
    /// <returns>The domain configuration builder for further configuration</returns>
    public static IDomainConfigurationBuilder WithDomainDrivenDesign(this IFSEntityFrameworkBuilder builder)
    {
        return new DomainConfigurationBuilder(builder);
    }
}

/// <summary>
/// Interface for configuring Domain-Driven Design functionality
/// </summary>
public interface IDomainConfigurationBuilder
{
    /// <summary>
    /// Gets the parent FS.EntityFramework builder
    /// </summary>
    IFSEntityFrameworkBuilder Builder { get; }

    /// <summary>
    /// Automatically registers domain repositories for all aggregate roots in the calling assembly
    /// </summary>
    /// <returns>The domain configuration builder for further configuration</returns>
    IDomainConfigurationBuilder WithAutoRepositoryDiscovery();

    /// <summary>
    /// Automatically registers domain repositories for all aggregate roots in the specified assembly
    /// </summary>
    /// <param name="assembly">The assembly to scan for aggregate roots</param>
    /// <returns>The domain configuration builder for further configuration</returns>
    IDomainConfigurationBuilder WithAutoRepositoryDiscovery(System.Reflection.Assembly assembly);

    /// <summary>
    /// Registers a domain repository for a specific aggregate root
    /// </summary>
    /// <typeparam name="TAggregate">The aggregate root type</typeparam>
    /// <typeparam name="TKey">The aggregate identifier type</typeparam>
    /// <returns>The domain configuration builder for further configuration</returns>
    IDomainConfigurationBuilder WithRepository<TAggregate, TKey>()
        where TAggregate : AggregateRoot<TKey>
        where TKey : IEquatable<TKey>;

    /// <summary>
    /// Registers a custom domain repository implementation
    /// </summary>
    /// <typeparam name="TAggregate">The aggregate root type</typeparam>
    /// <typeparam name="TKey">The aggregate identifier type</typeparam>
    /// <typeparam name="TRepository">The custom repository implementation</typeparam>
    /// <returns>The domain configuration builder for further configuration</returns>
    IDomainConfigurationBuilder WithCustomRepository<TAggregate, TKey, TRepository>()
        where TAggregate : AggregateRoot<TKey>
        where TKey : IEquatable<TKey>
        where TRepository : class, Domain.IDomainRepository<TAggregate, TKey>;

    /// <summary>
    /// Enables domain validation services
    /// </summary>
    /// <returns>The domain configuration builder for further configuration</returns>
    IDomainConfigurationBuilder WithDomainValidation();

    /// <summary>
    /// Completes the domain configuration and returns to the main builder
    /// </summary>
    /// <returns>The parent builder for method chaining</returns>
    IFSEntityFrameworkBuilder Complete();
}

/// <summary>
/// Internal implementation of the domain configuration builder
/// </summary>
internal class DomainConfigurationBuilder : IDomainConfigurationBuilder
{
    /// <summary>
    /// Initializes a new instance of the DomainConfigurationBuilder class
    /// </summary>
    /// <param name="builder">The parent FS.EntityFramework builder</param>
    public DomainConfigurationBuilder(IFSEntityFrameworkBuilder builder)
    {
        Builder = builder;

        // Register core domain services
        builder.Services.AddDomainServices();
    }

    /// <summary>
    /// Gets the parent FS.EntityFramework builder
    /// </summary>
    public IFSEntityFrameworkBuilder Builder { get; }

    /// <summary>
    /// Automatically registers domain repositories for all aggregate roots in the calling assembly
    /// </summary>
    /// <returns>The domain configuration builder for further configuration</returns>
    public IDomainConfigurationBuilder WithAutoRepositoryDiscovery()
    {
        Builder.Services.AddDomainRepositoriesFromCallingAssembly();
        return this;
    }

    /// <summary>
    /// Automatically registers domain repositories for all aggregate roots in the specified assembly
    /// </summary>
    /// <param name="assembly">The assembly to scan for aggregate roots</param>
    /// <returns>The domain configuration builder for further configuration</returns>
    public IDomainConfigurationBuilder WithAutoRepositoryDiscovery(System.Reflection.Assembly assembly)
    {
        Builder.Services.AddDomainRepositoriesFromAssembly(assembly);
        return this;
    }

    /// <summary>
    /// Registers a domain repository for a specific aggregate root
    /// </summary>
    /// <typeparam name="TAggregate">The aggregate root type</typeparam>
    /// <typeparam name="TKey">The aggregate identifier type</typeparam>
    /// <returns>The domain configuration builder for further configuration</returns>
    public IDomainConfigurationBuilder WithRepository<TAggregate, TKey>()
        where TAggregate : AggregateRoot<TKey>
        where TKey : IEquatable<TKey>
    {
        Builder.Services.AddDomainRepository<TAggregate, TKey>();
        return this;
    }

    /// <summary>
    /// Registers a custom domain repository implementation
    /// </summary>
    /// <typeparam name="TAggregate">The aggregate root type</typeparam>
    /// <typeparam name="TKey">The aggregate identifier type</typeparam>
    /// <typeparam name="TRepository">The custom repository implementation</typeparam>
    /// <returns>The domain configuration builder for further configuration</returns>
    public IDomainConfigurationBuilder WithCustomRepository<TAggregate, TKey, TRepository>()
        where TAggregate : AggregateRoot<TKey>
        where TKey : IEquatable<TKey>
        where TRepository : class, Domain.IDomainRepository<TAggregate, TKey>
    {
        Builder.Services.AddCustomDomainRepository<TAggregate, TKey, TRepository>();
        return this;
    }

    /// <summary>
    /// Enables domain validation services
    /// </summary>
    /// <returns>The domain configuration builder for further configuration</returns>
    public IDomainConfigurationBuilder WithDomainValidation()
    {
        Builder.Services.AddDomainValidation();
        return this;
    }

    /// <summary>
    /// Completes the domain configuration and returns to the main builder
    /// </summary>
    /// <returns>The parent builder for method chaining</returns>
    public IFSEntityFrameworkBuilder Complete()
    {
        return Builder;
    }
}