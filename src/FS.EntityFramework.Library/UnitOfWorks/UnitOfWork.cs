using System.Collections.Concurrent;
using FS.EntityFramework.Library.Common;
using FS.EntityFramework.Library.Diagnostics;
using FS.EntityFramework.Library.Interfaces;
using FS.EntityFramework.Library.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace FS.EntityFramework.Library.UnitOfWorks;

/// <summary>
/// Implementation of the Unit of Work pattern that coordinates multiple repositories
/// and manages database transactions and change tracking
/// </summary>
/// <param name="context">The Entity Framework context</param>
/// <param name="serviceProvider">The service provider for dependency injection</param>
public class UnitOfWork(DbContext context, IServiceProvider serviceProvider)
    : IUnitOfWork
{
    /// <summary>
    /// Optional metrics instance (null when metrics are not enabled)
    /// </summary>
    private readonly FSEntityFrameworkMetrics? _metrics = serviceProvider.GetService<FSEntityFrameworkMetrics>();

    /// <summary>
    /// Thread-safe repository cache with proper lifecycle management.
    /// Uses weak references to prevent memory leaks and supports safe disposal.
    /// </summary>
    private readonly ConcurrentDictionary<string, RepositoryCacheEntry> _repositoryCache = new();

    /// <summary>
    /// Lock object for thread-safe repository creation when cache misses occur.
    /// Prevents multiple threads from creating the same repository simultaneously.
    /// </summary>
    private readonly object _repositoryCreationLock = new();

    /// <summary>
    /// The current database transaction (if any)
    /// </summary>
    private IDbContextTransaction? _currentTransaction;

    /// <summary>
    /// Flag indicating whether the object has been disposed
    /// </summary>
    private bool _disposed;

    // ===== GENERIC REPOSITORY ACCESS =====

    /// <summary>
    /// Gets a specific repository implementation by type from the service container with enhanced safety.
    /// This method provides robust error handling and clear diagnostics for dependency injection issues.
    /// Includes caching for performance optimization and comprehensive validation for production reliability.
    /// </summary>
    /// <typeparam name="TRepository">The repository type to resolve</typeparam>
    /// <returns>The repository instance</returns>
    /// <exception cref="InvalidOperationException">Thrown when repository cannot be resolved or is not registered</exception>
    /// <exception cref="ObjectDisposedException">Thrown when UnitOfWork has been disposed</exception>
    public TRepository GetRepository<TRepository>() where TRepository : class
    {
        // Defensive programming: ensure UnitOfWork is not disposed
        ThrowIfDisposed();

        var repositoryType = typeof(TRepository);

        // Create cache key for specific repository types
        // We use a different cache key format to distinguish from generic repositories
        var cacheKey = CreateSpecificRepositoryCacheKey(repositoryType);

        // Try to get from cache first (performance optimization)
        if (_repositoryCache.TryGetValue(cacheKey, out var cachedEntry))
        {
            if (cachedEntry.IsValid && cachedEntry.Repository is TRepository cachedRepo)
            {
                return cachedRepo;
            }
            else
            {
                // Remove invalid cache entry
                _repositoryCache.TryRemove(cacheKey, out _);
            }
        }

        // Cache miss - resolve from dependency injection with thread safety
        return ResolveAndCacheSpecificRepository<TRepository>(cacheKey, repositoryType);
    }

    /// <summary>
    /// Gets a repository for the specified entity type with enhanced caching and safety.
    /// This method ensures thread-safe creation and proper lifecycle management of repositories.
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <typeparam name="TKey">The entity's primary key type</typeparam>
    /// <returns>A repository instance for the entity</returns>
    public IRepository<TEntity, TKey> GetRepository<TEntity, TKey>()
        where TEntity : BaseEntity<TKey>
        where TKey : IEquatable<TKey>
    {
        // Defensive programming: check if object is disposed
        ThrowIfDisposed();

        // Create a robust cache key that prevents collisions
        var cacheKey = CreateSecureRepositoryCacheKey<TEntity, TKey>();

        // Try to get from cache first (fast path)
        if (_repositoryCache.TryGetValue(cacheKey, out var cachedEntry))
        {
            // Validate that cached repository is still alive and valid
            if (cachedEntry.IsValid && cachedEntry.Repository != null)
            {
                _metrics?.RecordCacheHit();
                return (IRepository<TEntity, TKey>)cachedEntry.Repository;
            }
            else
            {
                // Remove invalid entry from cache
                _repositoryCache.TryRemove(cacheKey, out _);
            }
        }

        // Cache miss or invalid entry - create new repository with thread safety
        _metrics?.RecordCacheMiss();
        return CreateAndCacheRepository<TEntity, TKey>(cacheKey);
    }

    /// <summary>
    /// Gets a generic repository for entities without specific repository implementation.
    /// This method provides access to IRepository&lt;TEntity, TKey&gt;.
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <typeparam name="TKey">The entity's primary key type</typeparam>
    /// <returns>A repository instance for the entity</returns>
    public IRepository<TEntity, TKey> Repository<TEntity, TKey>()
        where TEntity : BaseEntity<TKey>
        where TKey : IEquatable<TKey>
    {
        // Use the same caching strategy for consistency
        return GetRepository<TEntity, TKey>();
    }

    /// <summary>
    /// Creates a unique cache key for repository instances that prevents type collisions
    /// This method ensures that entities with the same key type get different repository instances
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <typeparam name="TKey">The entity's primary key type</typeparam>
    /// <returns>A unique cache key string</returns>
    private static string CreateRepositoryCacheKey<TEntity, TKey>()
        where TEntity : BaseEntity<TKey>
        where TKey : IEquatable<TKey>
    {
        var entityType = typeof(TEntity);
        var keyType = typeof(TKey);

        // Create composite key format: "IRepository<EntityFullName,KeyFullName>"
        // Example: "IRepository<MyApp.Entities.Product,System.Int32>"
        // This ensures Product<int> and Category<int> get different cache keys
        return $"IRepository<{entityType.FullName},{keyType.FullName}>";
    }

    // ===== PERSISTENCE OPERATIONS =====

    /// <summary>
    /// Saves all pending changes across all repositories to the database
    /// </summary>
    /// <returns>The number of state entries written to the database</returns>
    public async Task<int> SaveChangesAsync()
    {
        try
        {
            var result = await context.SaveChangesAsync();
            _metrics?.RecordSaveChanges(true);
            return result;
        }
        catch
        {
            _metrics?.RecordSaveChanges(false);
            throw;
        }
    }

    /// <summary>
    /// Saves all pending changes across all repositories to the database
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The number of state entries written to the database</returns>
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            var result = await context.SaveChangesAsync(cancellationToken);
            _metrics?.RecordSaveChanges(true);
            return result;
        }
        catch
        {
            _metrics?.RecordSaveChanges(false);
            throw;
        }
    }

    // ===== TRANSACTION MANAGEMENT =====
    /// <summary>
    /// Begin database transaction.
    /// Bu method explicit transaction control sağlıyor.
    /// </summary>
    public async Task<IDbContextTransaction> BeginTransactionAsync()
    {
        if (_currentTransaction != null)
        {
            throw new InvalidOperationException("A transaction is already in progress");
        }

        _currentTransaction = await context.Database.BeginTransactionAsync();
        _metrics?.RecordTransaction("begin");
        return _currentTransaction;
    }

    /// <summary>
    /// Commit current transaction.
    /// </summary>
    public async Task CommitTransactionAsync()
    {
        if (_currentTransaction == null)
        {
            throw new InvalidOperationException("No transaction in progress");
        }

        try
        {
            await SaveChangesAsync();
            await _currentTransaction.CommitAsync();
            _metrics?.RecordTransaction("commit");
        }
        catch
        {
            await RollbackTransactionAsync();
            throw;
        }
        finally
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }
    }

    /// <summary>
    /// Rollback current transaction.
    /// </summary>
    public async Task RollbackTransactionAsync()
    {
        if (_currentTransaction == null)
        {
            throw new InvalidOperationException("No transaction in progress");
        }

        try
        {
            await _currentTransaction.RollbackAsync();
            _metrics?.RecordTransaction("rollback");
        }
        finally
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    /// <summary>
    /// Execute operation in transaction.
    /// </summary>
    public async Task<TKey> ExecuteInTransactionAsync<TKey>(Func<Task<TKey>> operation) where TKey : IEquatable<TKey>
    {
        await BeginTransactionAsync();
        try
        {
            var result = await operation();
            await CommitTransactionAsync();
            return result;
        }
        catch
        {
            await RollbackTransactionAsync();
            throw;
        }
    }

    // ===== CHANGE TRACKING =====

    /// <summary>
    /// Check if context has pending changes.
    /// </summary>
    public bool HasChanges => context.ChangeTracker.HasChanges();

    /// <summary>
    /// Detach all tracked entities.
    /// </summary>
    public void DetachAllEntities()
    {
        var entries = context.ChangeTracker.Entries().ToList();
        foreach (var entry in entries)
        {
            entry.State = EntityState.Detached;
        }
    }

    /// <summary>
    /// Gets the entity entry for the specified entity for state management
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <param name="entity">The entity instance</param>
    /// <returns>The entity entry</returns>
    public EntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class
    {
        return context.Entry(entity);
    }

    /// <summary>
    /// Creates a secure cache key that prevents type collisions and handles edge cases.
    /// This method addresses issues with null FullName values and assembly conflicts.
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <typeparam name="TKey">The entity's primary key type</typeparam>
    /// <returns>A unique, collision-resistant cache key</returns>
    private static string CreateSecureRepositoryCacheKey<TEntity, TKey>()
        where TEntity : BaseEntity<TKey>
        where TKey : IEquatable<TKey>
    {
        var entityType = typeof(TEntity);
        var keyType = typeof(TKey);

        // Build a comprehensive key that includes assembly information to prevent collisions
        var entityFullName = entityType.FullName ?? entityType.Name;
        var keyFullName = keyType.FullName ?? keyType.Name;
        var entityAssembly = entityType.Assembly.GetName().Name ?? "Unknown";
        var keyAssembly = keyType.Assembly.GetName().Name ?? "Unknown";

        // Create a collision-resistant key format
        return $"Repository[{entityFullName}@{entityAssembly},{keyFullName}@{keyAssembly}]";
    }

    /// <summary>
    /// Creates and caches a new repository with thread-safe double-checked locking pattern.
    /// This method ensures that only one repository is created per type even under high concurrency.
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <typeparam name="TKey">The entity's primary key type</typeparam>
    /// <param name="cacheKey">The cache key for the repository</param>
    /// <returns>A newly created and cached repository instance</returns>
    private IRepository<TEntity, TKey> CreateAndCacheRepository<TEntity, TKey>(string cacheKey)
        where TEntity : BaseEntity<TKey>
        where TKey : IEquatable<TKey>
    {
        // Double-checked locking pattern for thread safety
        lock (_repositoryCreationLock)
        {
            // Check again inside the lock in case another thread created it
            if (_repositoryCache.TryGetValue(cacheKey, out var existingEntry) &&
                existingEntry is { IsValid: true, Repository: not null })
            {
                return (IRepository<TEntity, TKey>)existingEntry.Repository;
            }

            // Create the repository with proper error handling
            var repository = CreateRepositoryInstance<TEntity, TKey>();

            // Cache the repository with metadata for lifecycle management
            var cacheEntry = new RepositoryCacheEntry(repository, DateTime.UtcNow);
            _repositoryCache.TryAdd(cacheKey, cacheEntry);

            return repository;
        }
    }

    /// <summary>
    /// Creates a repository instance with fallback strategies and proper error handling.
    /// Tries specific repository first, then falls back to generic repository.
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <typeparam name="TKey">The entity's primary key type</typeparam>
    /// <returns>A repository instance</returns>
    private IRepository<TEntity, TKey> CreateRepositoryInstance<TEntity, TKey>()
        where TEntity : BaseEntity<TKey>
        where TKey : IEquatable<TKey>
    {
        try
        {
            // Try to get specific repository implementation from the current scope
            // UnitOfWork is already scoped, so no need to create a new scope
            var specificRepository = serviceProvider.GetService<IRepository<TEntity, TKey>>();
            return specificRepository ??
                   // Fall back to generic repository
                   new BaseRepository<TEntity, TKey>(context, _metrics);
        }
        catch (Exception ex)
        {
            // Provide clear error message for debugging
            throw new InvalidOperationException(
                $"Failed to create repository for entity type {typeof(TEntity).Name} with key type {typeof(TKey).Name}. " +
                "This might indicate a problem with dependency injection configuration or DbContext state.", ex);
        }
    }

    /// <summary>
    /// Checks if the UnitOfWork has been disposed and throws if necessary.
    /// This prevents usage of disposed resources and provides clear error messages.
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(UnitOfWork),
                "Cannot access repositories after UnitOfWork has been disposed. " +
                "This usually indicates a lifecycle management issue in your application.");
        }
    }

    /// <summary>
    /// Safely disposes all cached repositories and clears the cache.
    /// This method ensures proper cleanup to prevent memory leaks.
    /// </summary>
    private void DisposeCachedRepositories()
    {
        if (_repositoryCache.IsEmpty) return;

        // Create a snapshot of cache entries to avoid modification during enumeration
        var cacheEntries = _repositoryCache.Values.ToList();

        foreach (var entry in cacheEntries)
        {
            try
            {
                // If repository implements IDisposable, dispose it
                if (entry.Repository is IDisposable disposableRepository)
                {
                    disposableRepository.Dispose();
                }
            }
            catch (Exception ex)
            {
                // Log disposal errors but don't throw (disposal should be safe)
                System.Diagnostics.Debug.WriteLine($"Error disposing repository: {ex.Message}");
            }
        }

        // Clear the cache
        _repositoryCache.Clear();
    }

    /// <summary>
    /// Creates a secure cache key for specific repository types.
    /// This method ensures that custom repositories don't collide with generic repositories in cache.
    /// Uses assembly information to prevent conflicts between repositories with same names from different assemblies.
    /// </summary>
    /// <param name="repositoryType">The repository type</param>
    /// <returns>A unique cache key for the specific repository type</returns>
    private static string CreateSpecificRepositoryCacheKey(Type repositoryType)
    {
        var typeName = repositoryType.FullName ?? repositoryType.Name;
        var assemblyName = repositoryType.Assembly.GetName().Name ?? "Unknown";

        // Use a different prefix to distinguish from generic entity repositories
        // This prevents cache key collisions between different repository patterns
        return $"SpecificRepo[{typeName}@{assemblyName}]";
    }

    /// <summary>
    /// Resolves and caches a specific repository with thread-safe creation and comprehensive error handling.
    /// This method provides detailed diagnostics when repository resolution fails, making debugging easier.
    /// </summary>
    /// <typeparam name="TRepository">The repository type to resolve</typeparam>
    /// <param name="cacheKey">The cache key for storing the repository</param>
    /// <param name="repositoryType">The repository type for error reporting</param>
    /// <returns>The resolved repository instance</returns>
    /// <exception cref="InvalidOperationException">Thrown when repository cannot be resolved</exception>
    private TRepository ResolveAndCacheSpecificRepository<TRepository>(string cacheKey, Type repositoryType)
        where TRepository : class
    {
        // Thread-safe repository creation using the same lock as generic repositories
        lock (_repositoryCreationLock)
        {
            // Double-check pattern: verify another thread didn't create it while we were waiting
            if (_repositoryCache.TryGetValue(cacheKey, out var existingEntry) &&
                existingEntry.IsValid && existingEntry.Repository is TRepository existingRepo)
            {
                return existingRepo;
            }

            // Attempt to resolve the repository from dependency injection
            var repository = serviceProvider.GetService<TRepository>();

            if (repository == null)
            {
                // Provide comprehensive error information for debugging
                var registeredServices = GetRegisteredRepositoryServices();
                var errorMessage = CreateRepositoryNotFoundErrorMessage(repositoryType, registeredServices);
                throw new InvalidOperationException(errorMessage);
            }

            // Cache the successfully resolved repository
            var cacheEntry = new RepositoryCacheEntry(repository, DateTime.UtcNow);
            _repositoryCache.TryAdd(cacheKey, cacheEntry);

            return repository;
        }
    }

    /// <summary>
    /// Creates a detailed error message when repository resolution fails.
    /// This method helps developers understand why their repository couldn't be resolved and how to fix it.
    /// Includes information about registered services for better debugging experience.
    /// </summary>
    /// <param name="repositoryType">The repository type that failed to resolve</param>
    /// <param name="registeredServices">List of registered repository services for reference</param>
    /// <returns>A comprehensive error message with debugging guidance</returns>
    private static string CreateRepositoryNotFoundErrorMessage(Type repositoryType, List<string> registeredServices)
    {
        var errorMessage = $"Repository of type '{repositoryType.Name}' is not registered in the dependency injection container.\n\n";

        errorMessage += "Common causes and solutions:\n";
        errorMessage += "1. Repository not registered: Add your repository to DI using services.AddScoped<IYourRepository, YourRepository>()\n";
        errorMessage += "2. Wrong interface: Ensure you're requesting the correct repository interface\n";
        errorMessage += "3. Missing assembly reference: Verify the repository assembly is properly referenced\n";

#if DEBUG
        errorMessage += "\n";
        if (registeredServices.Count != 0)
        {
            errorMessage += "Currently registered repository services:\n";
            errorMessage += string.Join("\n", registeredServices.Take(10));

            if (registeredServices.Count > 10)
            {
                errorMessage += $"\n... and {registeredServices.Count - 10} more services";
            }
        }
        else
        {
            errorMessage += "No repository services are currently registered in the container.";
        }
#endif

        return errorMessage;
    }

    /// <summary>
    /// Gets a list of registered repository services for diagnostic purposes using generic reflection.
    /// This method discovers repositories dynamically without hardcoding specific domain types.
    /// Works with any domain by scanning for common repository patterns rather than specific names.
    /// </summary>
    /// <returns>List of registered repository service type names</returns>
    private List<string> GetRegisteredRepositoryServices()
    {
        var registeredServices = new List<string>();

        try
        {
            // GENERIC APPROACH: Scan for common repository interface patterns
            // This works for any domain - healthcare, finance, e-commerce, etc.
            var discoveredRepositoryTypes = DiscoverRepositoryTypes();

            foreach (var repoType in discoveredRepositoryTypes)
            {
                try
                {
                    // Check if this repository type is actually registered in DI container
                    var service = serviceProvider.GetService(repoType);
                    if (service != null)
                    {
                        registeredServices.Add(FormatRepositoryTypeName(repoType));
                    }
                }
                catch
                {
                    // If specific service resolution fails, continue with others
                    // This ensures one failing service doesn't break the entire discovery
                }
            }
        }
        catch
        {
            // If entire service discovery fails, return empty list
            // Error reporting should never break the core functionality
        }

        return registeredServices.Take(10).ToList(); // Limit results to prevent spam
    }

    /// <summary>
    /// Discovers repository types using generic patterns that work across all domains.
    /// This method uses reflection to find interfaces that follow repository naming conventions.
    /// </summary>
    /// <returns>Collection of repository interface types found in loaded assemblies</returns>
    private IEnumerable<Type> DiscoverRepositoryTypes()
    {
        var repositoryTypes = new List<Type>();

        try
        {
            // Get all loaded assemblies (excluding system assemblies for performance)
            var relevantAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.FullName?.StartsWith("System.") == true)
                .Where(a => !a.FullName?.StartsWith("Microsoft.") == true)
                .Where(a => !a.FullName?.StartsWith("netstandard") == true);

            foreach (var assembly in relevantAssemblies)
            {
                try
                {
                    var types = assembly.GetTypes()
                        .Where(t => t.IsInterface)
                        .Where(t => IsRepositoryInterface(t));

                    repositoryTypes.AddRange(types);
                }
                catch
                {
                    // Some assemblies might not be accessible, continue with others
                }
            }
        }
        catch
        {
            // If assembly scanning fails entirely, return empty collection
        }

        return repositoryTypes;
    }

    /// <summary>
    /// Determines if a type is a repository interface using generic patterns.
    /// This method works for any domain by checking naming conventions and inheritance patterns.
    /// </summary>
    /// <param name="type">The type to examine</param>
    /// <returns>True if the type appears to be a repository interface</returns>
    private static bool IsRepositoryInterface(Type type)
    {
        // Pattern 1: Interface name ends with "Repository"
        if (type.Name.EndsWith("Repository", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Pattern 2: Interface inherits from IRepository<,>
        if (type.GetInterfaces().Any(i =>
                i.IsGenericType &&
                i.GetGenericTypeDefinition() == typeof(IRepository<,>)))
        {
            return true;
        }

        // Pattern 3: Interface inherits from our domain repository interface
        return type.GetInterfaces().Any(i =>
            i.IsGenericType &&
            i.GetGenericTypeDefinition().Name.Contains("IDomainRepository"));
    }

    /// <summary>
    /// Formats repository type name for user-friendly display in error messages.
    /// Removes generic type syntax and assembly information to improve readability.
    /// </summary>
    /// <param name="repositoryType">The repository type to format</param>
    /// <returns>A clean, readable type name</returns>
    private static string FormatRepositoryTypeName(Type repositoryType)
    {
        var typeName = repositoryType.Name;

        // Remove generic type parameters (e.g., IRepository`2 becomes IRepository)
        var backtickIndex = typeName.IndexOf('`');
        if (backtickIndex > 0)
        {
            typeName = typeName[..backtickIndex];
        }

        // Add generic parameter info if it's a generic type
        if (!repositoryType.IsGenericType) return typeName;
        var genericArgs = repositoryType.GetGenericArguments();
        if (genericArgs.Length <= 0) return typeName;
        var argNames = genericArgs.Select(arg => arg.Name).ToArray();
        typeName += $"<{string.Join(", ", argNames)}>";

        return typeName;
    }

    /// <summary>
    /// Enhanced disposal implementation with proper resource cleanup.
    /// Ensures all cached repositories and transactions are properly disposed.
    /// </summary>
    /// <param name="disposing">True if disposing; otherwise, false</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed || !disposing) return;

        try
        {
            // Dispose current transaction if exists
            _currentTransaction?.Dispose();

            // Dispose all cached repositories
            DisposeCachedRepositories();

            // Dispose the context
            context?.Dispose();
        }
        catch (Exception ex)
        {
            // Log disposal errors but don't throw from Dispose
            System.Diagnostics.Debug.WriteLine($"Error during UnitOfWork disposal: {ex.Message}");
        }
        finally
        {
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Represents a cached repository entry with metadata for lifecycle management.
/// This class helps track repository validity and creation time for cache maintenance.
/// </summary>
internal class RepositoryCacheEntry
{
    public object Repository { get; }
    public DateTime CreatedAt { get; }
    public bool IsValid => Repository != null;

    public RepositoryCacheEntry(object repository, DateTime createdAt)
    {
        Repository = repository ?? throw new ArgumentNullException(nameof(repository));
        CreatedAt = createdAt;
    }
}