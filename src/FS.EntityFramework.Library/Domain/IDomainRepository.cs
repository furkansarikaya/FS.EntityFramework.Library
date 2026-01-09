using System.Linq.Expressions;
using FS.EntityFramework.Library.Common;

namespace FS.EntityFramework.Library.Domain;

/// <summary>
/// Domain repository interface for aggregate roots
/// This interface should be implemented in the infrastructure layer
/// Keeps domain layer independent of infrastructure concerns
/// </summary>
/// <typeparam name="TAggregate">The aggregate root type</typeparam>
/// <typeparam name="TKey">The aggregate identifier type</typeparam>
public interface IDomainRepository<TAggregate, in TKey>
    where TAggregate : AggregateRoot<TKey>
    where TKey : IEquatable<TKey>
{
    /// <summary>
    /// Gets an aggregate by its identifier
    /// </summary>
    /// <param name="id">The aggregate identifier</param>
    /// <param name="includes">Include expressions for related data</param>
    /// <param name="disableTracking">Whether to disable change tracking</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The aggregate if found; otherwise null</returns>
    Task<TAggregate?> GetByIdAsync(TKey id,
        List<Expression<Func<TAggregate, object>>>? includes = null,
        bool disableTracking = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an aggregate by its identifier, throwing exception if not found
    /// </summary>
    /// <param name="id">The aggregate identifier</param>
    /// <param name="includes">Include expressions for related data</param>
    /// <param name="disableTracking">Whether to disable change tracking</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The aggregate</returns>
    /// <exception cref="InvalidOperationException">Thrown when aggregate not found</exception>
    Task<TAggregate> GetByIdRequiredAsync(TKey id,
        List<Expression<Func<TAggregate, object>>>? includes = null,
        bool disableTracking = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new aggregate to the repository
    /// </summary>
    /// <param name="aggregate">The aggregate to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task AddAsync(TAggregate aggregate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing aggregate in the repository
    /// </summary>
    /// <param name="aggregate">The aggregate to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task UpdateAsync(TAggregate aggregate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes an aggregate from the repository
    /// </summary>
    /// <param name="aggregate">The aggregate to remove</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task RemoveAsync(TAggregate aggregate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds aggregates that satisfy the given specification
    /// </summary>
    /// <param name="specification">The specification to apply</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of aggregates matching the specification</returns>
    Task<IEnumerable<TAggregate>> FindAsync(ISpecification<TAggregate> specification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if any aggregate satisfies the given specification
    /// </summary>
    /// <param name="specification">The specification to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if any aggregate matches; otherwise false</returns>
    Task<bool> AnyAsync(ISpecification<TAggregate> specification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts aggregates that satisfy the given specification
    /// </summary>
    /// <param name="specification">The specification to apply</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The count of matching aggregates</returns>
    Task<int> CountAsync(ISpecification<TAggregate> specification, CancellationToken cancellationToken = default);
}