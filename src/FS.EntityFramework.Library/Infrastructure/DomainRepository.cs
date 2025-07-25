using FS.EntityFramework.Library.Common;
using FS.EntityFramework.Library.UnitOfWorks;

namespace FS.EntityFramework.Library.Infrastructure;

/// <summary>
/// Domain repository implementation that wraps the existing EF repository
/// Provides domain-centric operations while leveraging existing infrastructure
/// </summary>
/// <typeparam name="TAggregate">The aggregate root type</typeparam>
/// <typeparam name="TKey">The aggregate identifier type</typeparam>
public class DomainRepository<TAggregate, TKey> : Domain.IDomainRepository<TAggregate, TKey>
    where TAggregate : AggregateRoot<TKey>
    where TKey : IEquatable<TKey>
{ 
    private readonly IUnitOfWork _unitOfWork;
    private readonly Interfaces.IRepository<TAggregate, TKey> _efRepository;
    private readonly Common.IDomainEvent? _domainEventPublisher;

    /// <summary>
    /// Initializes a new instance of the DomainRepository class
    /// </summary>
    /// <param name="unitOfWork">The unit of work</param>
    /// <param name="domainEventPublisher">Optional domain event publisher</param>
    public DomainRepository(
        IUnitOfWork unitOfWork,
        Common.IDomainEvent? domainEventPublisher = null)
    {
        _unitOfWork = unitOfWork;
        
        //Lazy initialization
        _efRepository = _unitOfWork.GetRepository<TAggregate, TKey>();
        _domainEventPublisher = domainEventPublisher;
    }

    /// <summary>
    /// Gets an aggregate by its identifier
    /// </summary>
    /// <param name="id">The aggregate identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The aggregate if found; otherwise null</returns>
    public async Task<TAggregate?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default)
    {
        return await _efRepository.GetByIdAsync(id, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Gets an aggregate by its identifier, throwing exception if not found
    /// </summary>
    /// <param name="id">The aggregate identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The aggregate</returns>
    /// <exception cref="Domain.AggregateNotFoundException">Thrown when aggregate not found</exception>
    public async Task<TAggregate> GetByIdRequiredAsync(TKey id, CancellationToken cancellationToken = default)
    {
        var aggregate = await GetByIdAsync(id, cancellationToken);
        if (aggregate == null)
        {
            throw new Domain.AggregateNotFoundException(typeof(TAggregate), id!);
        }

        return aggregate;
    }

    /// <summary>
    /// Adds a new aggregate to the repository
    /// </summary>
    /// <param name="aggregate">The aggregate to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task AddAsync(TAggregate aggregate, CancellationToken cancellationToken = default)
    {
        await _efRepository.AddAsync(aggregate, saveChanges: false, cancellationToken);
    }

    /// <summary>
    /// Updates an existing aggregate in the repository
    /// </summary>
    /// <param name="aggregate">The aggregate to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task UpdateAsync(TAggregate aggregate, CancellationToken cancellationToken = default)
    {
        await _efRepository.UpdateAsync(aggregate, saveChanges: false, cancellationToken);
    }

    /// <summary>
    /// Removes an aggregate from the repository
    /// </summary>
    /// <param name="aggregate">The aggregate to remove</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task RemoveAsync(TAggregate aggregate, CancellationToken cancellationToken = default)
    {
        await _efRepository.DeleteAsync(aggregate, saveChanges: false, cancellationToken);
    }

    /// <summary>
    /// Finds aggregates that satisfy the given specification
    /// </summary>
    /// <param name="specification">The specification to apply</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of aggregates matching the specification</returns>
    public async Task<IEnumerable<TAggregate>> FindAsync(Domain.ISpecification<TAggregate> specification, CancellationToken cancellationToken = default)
    {
        var query = _efRepository.GetQueryable();
        var filteredQuery = query.Where(specification.ToExpression());
        return await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(filteredQuery, cancellationToken);
    }

    /// <summary>
    /// Checks if any aggregate satisfies the given specification
    /// </summary>
    /// <param name="specification">The specification to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if any aggregate matches; otherwise false</returns>
    public async Task<bool> AnyAsync(Domain.ISpecification<TAggregate> specification, CancellationToken cancellationToken = default)
    {
        var query = _efRepository.GetQueryable();
        return await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AnyAsync(query, specification.ToExpression(), cancellationToken);
    }

    /// <summary>
    /// Counts aggregates that satisfy the given specification
    /// </summary>
    /// <param name="specification">The specification to apply</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The count of matching aggregates</returns>
    public async Task<int> CountAsync(Domain.ISpecification<TAggregate> specification, CancellationToken cancellationToken = default)
    {
        var query = _efRepository.GetQueryable();
        return await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(query, specification.ToExpression(), cancellationToken);
    }
}