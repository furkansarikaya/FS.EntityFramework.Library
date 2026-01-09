using System.Linq.Expressions;
using System.Reflection;
using FS.EntityFramework.Library.Common;
using FS.EntityFramework.Library.UnitOfWorks;
using Microsoft.EntityFrameworkCore;

namespace FS.EntityFramework.Library.Infrastructure;

/// <summary>
/// Enhanced domain repository implementation with comprehensive specification support.
/// Provides domain-centric operations with ordering, pagination, projection, and eager loading capabilities.
/// </summary>
/// <typeparam name="TAggregate">The aggregate root type</typeparam>
/// <typeparam name="TKey">The aggregate identifier type</typeparam>
public class DomainRepository<TAggregate, TKey> : Domain.IDomainRepository<TAggregate, TKey>
    where TAggregate : AggregateRoot<TKey>
    where TKey : IEquatable<TKey>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly Interfaces.IRepository<TAggregate, TKey> _efRepository;
    private readonly DbContext _context;

    private static readonly MethodInfo? _asSplitQueryMethod;

    /// <summary>
    /// Static constructor to detect AsSplitQuery availability at runtime
    /// This allows the library to work with both relational and non-relational providers
    /// </summary>
    static DomainRepository()
    {
        // Try to find AsSplitQuery method via reflection
        // This makes the library compatible with all EF Core providers
        try
        {
            var relationalExtensionsType = Type.GetType(
                "Microsoft.EntityFrameworkCore.RelationalQueryableExtensions, Microsoft.EntityFrameworkCore.Relational");

            if (relationalExtensionsType != null)
            {
                _asSplitQueryMethod = relationalExtensionsType.GetMethod(
                    "AsSplitQuery",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new[] { typeof(IQueryable<>).MakeGenericType(typeof(TAggregate)) },
                    null);
            }
        }
        catch
        {
            // AsSplitQuery not available - will be silently ignored
            _asSplitQueryMethod = null;
        }
    }

    /// <summary>
    /// Initializes a new instance of the DomainRepository class
    /// </summary>
    /// <param name="unitOfWork">The unit of work</param>
    /// <param name="context">The DbContext instance</param>
    public DomainRepository(IUnitOfWork unitOfWork, DbContext context)
    {
        _unitOfWork = unitOfWork;
        _context = context;
        _efRepository = _unitOfWork.GetRepository<TAggregate, TKey>();
    }

    /// <summary>
    /// Gets an aggregate by its identifier
    /// </summary>
    public async Task<TAggregate?> GetByIdAsync(TKey id,
        List<Expression<Func<TAggregate, object>>>? includes = null,
        bool disableTracking = true,
        CancellationToken cancellationToken = default)
    {
        return await _efRepository.GetByIdAsync(id,
            includes: includes,
            disableTracking: disableTracking,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Gets an aggregate by its identifier, throwing exception if not found
    /// </summary>
    public async Task<TAggregate> GetByIdRequiredAsync(TKey id,
        List<Expression<Func<TAggregate, object>>>? includes = null,
        bool disableTracking = true,
        CancellationToken cancellationToken = default)
    {
        var aggregate = await GetByIdAsync(id,
            includes: includes,
            disableTracking: disableTracking,
            cancellationToken: cancellationToken);
        return aggregate == null ? throw new Domain.AggregateNotFoundException(typeof(TAggregate), id!) : aggregate;
    }

    /// <summary>
    /// Adds a new aggregate to the repository
    /// </summary>
    public async Task AddAsync(TAggregate aggregate, CancellationToken cancellationToken = default)
    {
        await _efRepository.AddAsync(aggregate, saveChanges: false, cancellationToken);
    }

    /// <summary>
    /// Updates an existing aggregate in the repository
    /// </summary>
    public async Task UpdateAsync(TAggregate aggregate, CancellationToken cancellationToken = default)
    {
        await _efRepository.UpdateAsync(aggregate, saveChanges: false, cancellationToken);
    }

    /// <summary>
    /// Removes an aggregate from the repository
    /// </summary>
    public async Task RemoveAsync(TAggregate aggregate, CancellationToken cancellationToken = default)
    {
        await _efRepository.DeleteAsync(aggregate, saveChanges: false, cancellationToken);
    }

    /// <summary>
    /// Finds aggregates that satisfy the given specification with comprehensive query support.
    /// Automatically applies all includes, ordering, pagination, and other query configurations.
    /// </summary>
    /// <param name="specification">The specification to apply</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of aggregates matching the specification</returns>
    public async Task<IEnumerable<TAggregate>> FindAsync(Domain.ISpecification<TAggregate> specification, CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(specification);
        return await query.ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Checks if any aggregate satisfies the given specification.
    /// Note: Only applies the predicate, ignores includes and ordering for optimal performance.
    /// </summary>
    public async Task<bool> AnyAsync(Domain.ISpecification<TAggregate> specification, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<TAggregate>().AsQueryable();

        // Apply query filters setting
        if (specification.IgnoreQueryFilters)
        {
            query = query.IgnoreQueryFilters();
        }

        // Apply the predicate
        query = query.Where(specification.ToExpression());

        // Apply search if configured
        if (!string.IsNullOrWhiteSpace(specification.SearchTerm) &&
            specification.SearchProperties != null &&
            specification.SearchProperties.Any())
        {
            query = ApplySearch(query, specification);
        }

        return await query.AnyAsync(cancellationToken);
    }

    /// <summary>
    /// Counts aggregates that satisfy the given specification.
    /// Note: Only applies the predicate, ignores includes and ordering for optimal performance.
    /// </summary>
    public async Task<int> CountAsync(Domain.ISpecification<TAggregate> specification, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<TAggregate>().AsQueryable();

        // Apply query filters setting
        if (specification.IgnoreQueryFilters)
        {
            query = query.IgnoreQueryFilters();
        }

        // Apply the predicate
        query = query.Where(specification.ToExpression());

        // Apply search if configured
        if (!string.IsNullOrWhiteSpace(specification.SearchTerm) &&
            specification.SearchProperties != null &&
            specification.SearchProperties.Any())
        {
            query = ApplySearch(query, specification);
        }

        return await query.CountAsync(cancellationToken);
    }

    /// <summary>
    /// Builds a complete query from the specification with all configurations applied.
    /// This is the core method that transforms a specification into an executable query.
    /// </summary>
    /// <param name="specification">The specification to build query from</param>
    /// <returns>A configured IQueryable ready for execution</returns>
    private IQueryable<TAggregate> BuildQuery(Domain.ISpecification<TAggregate> specification)
    {
        // Start with DbSet to get proper IQueryable<T>
        IQueryable<TAggregate> query = _context.Set<TAggregate>();

        // Apply tracking configuration
        if (specification.DisableTracking)
        {
            query = query.AsNoTracking();
        }

        // 1. Apply query filters setting
        if (specification.IgnoreQueryFilters)
        {
            query = query.IgnoreQueryFilters();
        }

        // 2. Apply split query setting (must be before includes)
        // Uses reflection to support both relational and non-relational providers
        if (specification.AsSplitQuery)
        {
            query = ApplySplitQueryIfAvailable(query);
        }

        // 3. Apply the core predicate
        query = query.Where(specification.ToExpression());

        // 4. Apply search if configured
        if (!string.IsNullOrWhiteSpace(specification.SearchTerm) &&
            specification.SearchProperties != null &&
            specification.SearchProperties.Any())
        {
            query = ApplySearch(query, specification);
        }

        // 5. Apply expression-based includes
        foreach (var include in specification.Includes)
        {
            query = query.Include(include);
        }

        // 6. Apply string-based includes
        foreach (var includeString in specification.IncludeStrings)
        {
            query = query.Include(includeString);
        }

        // 7. Apply grouping if configured
        if (specification.GroupBy != null)
        {
            query = query.GroupBy(specification.GroupBy).SelectMany(g => g);
        }

        // 8. Apply ordering
        query = ApplyOrdering(query, specification);

        // 9. Apply pagination if enabled
        if (specification.IsPagingEnabled)
        {
            query = query.Skip(specification.Skip).Take(specification.Take);
        }

        return query;
    }

    /// <summary>
    /// Applies split query optimization if available in the current EF Core provider.
    /// This method uses reflection to ensure compatibility with both relational and non-relational providers.
    /// Split query prevents cartesian explosion when loading multiple collections.
    /// </summary>
    /// <param name="query">The query to apply split query to</param>
    /// <returns>The query with split query applied, or the original query if not supported</returns>
    private static IQueryable<TAggregate> ApplySplitQueryIfAvailable(IQueryable<TAggregate> query)
    {
        if (_asSplitQueryMethod == null)
        {
            // AsSplitQuery is not available (non-relational provider or old EF version)
            // Return query unchanged
            return query;
        }

        try
        {
            // Invoke AsSplitQuery via reflection
            var genericMethod = _asSplitQueryMethod.MakeGenericMethod(typeof(TAggregate));
            var result = genericMethod.Invoke(null, [query]);
            return (IQueryable<TAggregate>)result!;
        }
        catch
        {
            // If invocation fails for any reason, return original query
            // This ensures the library continues to work even if split query fails
            return query;
        }
    }

    /// <summary>
    /// Applies all ordering expressions from the specification to the query.
    /// Handles multiple order expressions correctly (OrderBy → ThenBy chain).
    /// </summary>
    /// <param name="query">The query to apply ordering to</param>
    /// <param name="specification">The specification containing ordering expressions</param>
    /// <returns>The query with ordering applied</returns>
    private static IQueryable<TAggregate> ApplyOrdering(
        IQueryable<TAggregate> query,
        Domain.ISpecification<TAggregate> specification)
    {
        if (!specification.OrderExpressions.Any())
        {
            return query;
        }

        IOrderedQueryable<TAggregate>? orderedQuery = null;

        // First ordering expression uses OrderBy/OrderByDescending
        var firstOrder = specification.OrderExpressions[0];
        orderedQuery = firstOrder.Ascending
            ? query.OrderBy(firstOrder.KeySelector)
            : query.OrderByDescending(firstOrder.KeySelector);

        // Subsequent ordering expressions use ThenBy/ThenByDescending
        for (var i = 1; i < specification.OrderExpressions.Count; i++)
        {
            var orderExpr = specification.OrderExpressions[i];
            orderedQuery = orderExpr.Ascending
                ? orderedQuery.ThenBy(orderExpr.KeySelector)
                : orderedQuery.ThenByDescending(orderExpr.KeySelector);
        }

        return orderedQuery;
    }

    /// <summary>
    /// Applies text-based search across multiple properties using OR logic.
    /// Search is case-insensitive and uses Contains semantics.
    /// </summary>
    /// <param name="query">The query to apply search to</param>
    /// <param name="specification">The specification containing search configuration</param>
    /// <returns>The query with search filter applied</returns>
    private static IQueryable<TAggregate> ApplySearch(
        IQueryable<TAggregate> query,
        Domain.ISpecification<TAggregate> specification)
    {
        if (string.IsNullOrWhiteSpace(specification.SearchTerm) ||
            specification.SearchProperties == null ||
            !specification.SearchProperties.Any())
        {
            return query;
        }

        // Build a combined OR expression for all search properties
        var parameter = Expression.Parameter(typeof(TAggregate), "x");
        Expression? searchExpression = null;

        var searchTerm = specification.SearchTerm.ToLower();
        var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) })!;
        var toLowerMethod = typeof(string).GetMethod("ToLower", Type.EmptyTypes)!;

        foreach (var propertySelector in specification.SearchProperties)
        {
            // Get the property from the expression by replacing the parameter
            var propertyExpression = new ParameterReplacer(parameter).Visit(propertySelector.Body);

// Ensure it's a string property
            if (propertyExpression.Type != typeof(string))
            {
                // Try to convert to string
                var toStringMethod = propertyExpression.Type.GetMethod("ToString", Type.EmptyTypes);
                if (toStringMethod != null)
                {
                    propertyExpression = Expression.Call(propertyExpression, toStringMethod);
                }
                else
                {
                    continue; // Skip non-string properties without ToString
                }
            }


            // Create null check
            var nullCheck = Expression.NotEqual(
                propertyExpression,
                Expression.Constant(null, typeof(string)));

            // Create: property.ToLower().Contains(searchTerm)
            var toLowerCall = Expression.Call(propertyExpression, toLowerMethod);
            var containsCall = Expression.Call(
                toLowerCall,
                containsMethod,
                Expression.Constant(searchTerm));

            // Combine with null check: property != null && property.ToLower().Contains(searchTerm)
            var propertyCondition = Expression.AndAlso(nullCheck, containsCall);

            // Combine with OR
            searchExpression = searchExpression == null
                ? propertyCondition
                : Expression.OrElse(searchExpression, propertyCondition);
        }

        if (searchExpression != null)
        {
            var searchLambda = Expression.Lambda<Func<TAggregate, bool>>(searchExpression, parameter);
            query = query.Where(searchLambda);
        }

        return query;
    }

    private class ParameterReplacer(ParameterExpression parameter) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
        {
            return parameter;
        }
    }
}