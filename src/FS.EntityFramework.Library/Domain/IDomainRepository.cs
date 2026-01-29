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
    /// <param name="disableTracking">Whether to disable change tracking (defaults to false for update scenarios)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The aggregate if found; otherwise null</returns>
    Task<TAggregate?> GetByIdAsync(TKey id,
        List<Expression<Func<TAggregate, object>>>? includes = null,
        bool disableTracking = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an aggregate by its identifier, throwing exception if not found
    /// </summary>
    /// <param name="id">The aggregate identifier</param>
    /// <param name="includes">Include expressions for related data</param>
    /// <param name="disableTracking">Whether to disable change tracking (defaults to false for update scenarios)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The aggregate</returns>
    /// <exception cref="InvalidOperationException">Thrown when aggregate not found</exception>
    Task<TAggregate> GetByIdRequiredAsync(TKey id,
        List<Expression<Func<TAggregate, object>>>? includes = null,
        bool disableTracking = false,
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

    #region Single Entity Methods

    /// <summary>
    /// Gets the first aggregate matching the specification.
    /// </summary>
    /// <param name="specification">The specification to apply.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The first matching aggregate or null if none found.</returns>
    Task<TAggregate?> FirstOrDefaultAsync(
        ISpecification<TAggregate> specification,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the single aggregate matching the specification.
    /// Throws if zero or more than one aggregate matches.
    /// </summary>
    /// <param name="specification">The specification to apply.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The single matching aggregate or null if none found.</returns>
    /// <exception cref="InvalidOperationException">Thrown when more than one aggregate matches.</exception>
    Task<TAggregate?> SingleOrDefaultAsync(
        ISpecification<TAggregate> specification,
        CancellationToken cancellationToken = default);

    #endregion

    #region Projection Methods

    /// <summary>
    /// Gets an aggregate by its identifier and projects it to the specified result type.
    /// Ideal for read-only scenarios where you need specific fields from an aggregate.
    /// </summary>
    /// <typeparam name="TResult">The type to project the aggregate to.</typeparam>
    /// <param name="id">The aggregate identifier.</param>
    /// <param name="selector">A projection expression defining the transformation.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The projected result if found; otherwise null.</returns>
    Task<TResult?> GetByIdAsync<TResult>(
        TKey id,
        Expression<Func<TAggregate, TResult>> selector,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds aggregates matching the specification and projects each to the specified result type.
    /// Combines specifications with efficient projection for CQRS architectures.
    /// </summary>
    /// <typeparam name="TResult">The type to project each aggregate to.</typeparam>
    /// <param name="specification">The specification containing filtering, ordering, and pagination.</param>
    /// <param name="selector">A projection expression defining the transformation.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A collection of projected results matching the specification.</returns>
    Task<IEnumerable<TResult>> FindAsync<TResult>(
        ISpecification<TAggregate> specification,
        Expression<Func<TAggregate, TResult>> selector,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds aggregates matching the specification and uses the specification's built-in selector.
    /// The selector must be defined in the specification using ApplySelector&lt;TResult&gt;().
    /// </summary>
    /// <typeparam name="TResult">The projection result type (must match specification's selector type).</typeparam>
    /// <param name="specification">The specification with a defined selector.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A collection of projected results.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the specification does not have a selector or the selector type doesn't match TResult.
    /// </exception>
    Task<IEnumerable<TResult>> FindWithSelectorAsync<TResult>(
        ISpecification<TAggregate> specification,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the first aggregate matching the specification and projects it.
    /// </summary>
    /// <typeparam name="TResult">The type to project the aggregate to.</typeparam>
    /// <param name="specification">The specification to apply.</param>
    /// <param name="selector">A projection expression defining the transformation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The projected result or null if none found.</returns>
    Task<TResult?> FirstOrDefaultAsync<TResult>(
        ISpecification<TAggregate> specification,
        Expression<Func<TAggregate, TResult>> selector,
        CancellationToken cancellationToken = default);

    #endregion

    #region Paginated Results

    /// <summary>
    /// Finds aggregates matching the specification and returns a paginated result.
    /// </summary>
    /// <param name="specification">The specification to apply.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paginated result with items and metadata.</returns>
    Task<Paging.IPaginate<TAggregate>> FindPagedAsync(
        ISpecification<TAggregate> specification,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds aggregates matching the specification, projects them, and returns a paginated result.
    /// </summary>
    /// <typeparam name="TResult">The type to project each aggregate to.</typeparam>
    /// <param name="specification">The specification to apply.</param>
    /// <param name="selector">A projection expression defining the transformation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paginated result with projected items and metadata.</returns>
    Task<Paging.IPaginate<TResult>> FindPagedAsync<TResult>(
        ISpecification<TAggregate> specification,
        Expression<Func<TAggregate, TResult>> selector,
        CancellationToken cancellationToken = default);

    #endregion
}