using System.Linq.Expressions;
using System.Reflection;
using FS.EntityFramework.Library.Common;
using FS.EntityFramework.Library.Paging;
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
    private static readonly MethodInfo? _tagWithMethod;

    /// <summary>
    /// Static constructor to detect AsSplitQuery and TagWith availability at runtime
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
                    [typeof(IQueryable<>).MakeGenericType(typeof(TAggregate))],
                    null);
            }
        }
        catch
        {
            // AsSplitQuery not available - will be silently ignored
            _asSplitQueryMethod = null;
        }

        // Try to find TagWith method
        try
        {
            _tagWithMethod = typeof(EntityFrameworkQueryableExtensions).GetMethod(
                "TagWith",
                BindingFlags.Public | BindingFlags.Static,
                null,
                [typeof(IQueryable<>).MakeGenericType(typeof(TAggregate)), typeof(string)],
                null);
        }
        catch
        {
            _tagWithMethod = null;
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
        bool disableTracking = false,
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
        bool disableTracking = false,
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

        if (specification.IgnoreQueryFilters)
            query = query.IgnoreQueryFilters();

        query = query.Where(specification.ToExpression());

        foreach (var criteria in specification.AdditionalCriteria)
            query = query.Where(criteria);

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

        if (specification.IgnoreQueryFilters)
            query = query.IgnoreQueryFilters();

        query = query.Where(specification.ToExpression());

        foreach (var criteria in specification.AdditionalCriteria)
            query = query.Where(criteria);

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
            query = specification.UseIdentityResolution
                ? query.AsNoTrackingWithIdentityResolution()
                : query.AsNoTracking();
        }

        // 1. Apply query filters setting
        if (specification.IgnoreQueryFilters)
        {
            query = query.IgnoreQueryFilters();
        }

        // 2. Apply query tag for debugging
        if (!string.IsNullOrWhiteSpace(specification.QueryTag))
        {
            query = query.TagWith(specification.QueryTag);
        }

        // 3. Apply split query setting (must be before includes)
        // Uses reflection to support both relational and non-relational providers
        if (specification.AsSplitQuery)
        {
            query = ApplySplitQueryIfAvailable(query);
        }

        // 4. Apply the core predicate
        query = query.Where(specification.ToExpression());

        // 4.5. Apply additional criteria
        foreach (var criteria in specification.AdditionalCriteria)
        {
            query = query.Where(criteria);
        }

        // 5. Apply search if configured
        if (!string.IsNullOrWhiteSpace(specification.SearchTerm) &&
            specification.SearchProperties != null &&
            specification.SearchProperties.Any())
        {
            query = ApplySearch(query, specification);
        }

        // 6. Apply expression-based includes (simple)
        foreach (var include in specification.Includes)
        {
            query = query.Include(include);
        }

        // 7. Apply advanced includes with ThenInclude support
        query = ApplyAdvancedIncludes(query, specification);

        // 8. Apply string-based includes
        foreach (var includeString in specification.IncludeStrings)
        {
            query = query.Include(includeString);
        }

        // 9. Apply grouping if configured
        if (specification.GroupBy != null)
        {
            query = query.GroupBy(specification.GroupBy).SelectMany(g => g);
        }

        // 10. Apply distinct if configured
        if (specification.IsDistinct)
        {
            query = query.Distinct();
        }

        // 11. Apply ordering
        query = ApplyOrdering(query, specification);

        // 12. Apply pagination if enabled
        if (specification.IsPagingEnabled)
        {
            query = query.Skip(specification.Skip).Take(specification.Take);
        }
        // 13. Apply limit if set (without pagination)
        else if (specification.Limit.HasValue)
        {
            query = query.Take(specification.Limit.Value);
        }

        return query;
    }

    /// <summary>
    /// Applies advanced includes with ThenInclude support using reflection.
    /// </summary>
    private IQueryable<TAggregate> ApplyAdvancedIncludes(
        IQueryable<TAggregate> query,
        Domain.ISpecification<TAggregate> specification)
    {
        if (!specification.IncludeExpressions.Any())
            return query;

        // Group include expressions by their root (expressions without previous include)
        var rootIncludes = specification.IncludeExpressions
            .Where(e => e.PreviousInclude == null)
            .ToList();

        foreach (var rootInclude in rootIncludes)
        {
            query = ApplyIncludeChain(query, rootInclude, specification.IncludeExpressions.ToList());
        }

        return query;
    }

    /// <summary>
    /// Applies an include chain starting from a root include expression.
    /// </summary>
    private IQueryable<TAggregate> ApplyIncludeChain(
        IQueryable<TAggregate> query,
        Domain.IncludeExpressionInfo rootInclude,
        List<Domain.IncludeExpressionInfo> allIncludes)
    {
        // Apply the root include
        var includeMethod = typeof(EntityFrameworkQueryableExtensions)
            .GetMethods()
            .First(m => m.Name == "Include" && m.GetParameters().Length == 2 &&
                       m.GetParameters()[1].ParameterType.GetGenericTypeDefinition() == typeof(Expression<>));

        // Use expression's ReturnType instead of PropertyType:
        // For reference includes: ReturnType = PropertyType (e.g., Customer)
        // For collection includes: ReturnType = IEnumerable<PropertyType> (e.g., IEnumerable<OrderItem>)
        // This ensures the generic type matches the expression's delegate signature
        var genericIncludeMethod = includeMethod.MakeGenericMethod(typeof(TAggregate), rootInclude.Expression.ReturnType);

        var includableQuery = genericIncludeMethod.Invoke(null, [query, rootInclude.Expression]);

        // Find and apply ThenIncludes
        var childIncludes = allIncludes
            .Where(e => e.PreviousInclude == rootInclude)
            .ToList();

        foreach (var childInclude in childIncludes)
        {
            includableQuery = ApplyThenIncludeChain(includableQuery!, rootInclude, childInclude, allIncludes);
        }

        return (IQueryable<TAggregate>)includableQuery!;
    }

    /// <summary>
    /// Applies ThenInclude recursively.
    /// </summary>
    private object ApplyThenIncludeChain(
        object includableQuery,
        Domain.IncludeExpressionInfo parentInclude,
        Domain.IncludeExpressionInfo currentInclude,
        List<Domain.IncludeExpressionInfo> allIncludes)
    {
        // Get the appropriate ThenInclude method based on whether parent is collection
        var thenIncludeMethods = typeof(EntityFrameworkQueryableExtensions)
            .GetMethods()
            .Where(m => m.Name == "ThenInclude")
            .ToList();

        MethodInfo? thenIncludeMethod;

        if (parentInclude.IsCollection)
        {
            // ThenInclude for collection: IIncludableQueryable<TEntity, IEnumerable<TPreviousProperty>>
            thenIncludeMethod = thenIncludeMethods.FirstOrDefault(m =>
            {
                var parameters = m.GetParameters();
                if (parameters.Length != 2) return false;
                var paramType = parameters[0].ParameterType;
                if (!paramType.IsGenericType) return false;
                var genericArgs = paramType.GetGenericArguments();
                return genericArgs.Length == 2 && genericArgs[1].IsGenericType &&
                       genericArgs[1].GetGenericTypeDefinition() == typeof(IEnumerable<>);
            });
        }
        else
        {
            // ThenInclude for reference
            thenIncludeMethod = thenIncludeMethods.FirstOrDefault(m =>
            {
                var parameters = m.GetParameters();
                if (parameters.Length != 2) return false;
                var paramType = parameters[0].ParameterType;
                if (!paramType.IsGenericType) return false;
                var genericArgs = paramType.GetGenericArguments();
                return genericArgs.Length == 2 && !genericArgs[1].IsGenericType;
            });
        }

        if (thenIncludeMethod == null)
            return includableQuery;

        // parentInclude.PropertyType is the element type (e.g., OrderItem) which is correct
        // for TPreviousProperty - the IEnumerable<> wrapper is in the method's parameter type.
        // currentInclude uses Expression.ReturnType to handle collection ThenIncludes:
        // For reference: ReturnType = PropertyType (e.g., Product)
        // For collection: ReturnType = IEnumerable<PropertyType> (e.g., IEnumerable<Tag>)
        var genericThenIncludeMethod = thenIncludeMethod.MakeGenericMethod(
            typeof(TAggregate),
            parentInclude.PropertyType,
            currentInclude.Expression.ReturnType);

        var newIncludableQuery = genericThenIncludeMethod.Invoke(null, [includableQuery, currentInclude.Expression]);

        // Apply child ThenIncludes recursively
        var childIncludes = allIncludes
            .Where(e => e.PreviousInclude == currentInclude)
            .ToList();

        foreach (var childInclude in childIncludes)
        {
            newIncludableQuery = ApplyThenIncludeChain(newIncludableQuery!, currentInclude, childInclude, allIncludes);
        }

        return newIncludableQuery!;
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
        var containsMethod = typeof(string).GetMethod("Contains", [typeof(string)])!;
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

    #region Single Entity Methods

    /// <inheritdoc />
    public async Task<TAggregate?> FirstOrDefaultAsync(
        Domain.ISpecification<TAggregate> specification,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(specification);
        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TAggregate?> SingleOrDefaultAsync(
        Domain.ISpecification<TAggregate> specification,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(specification);
        return await query.SingleOrDefaultAsync(cancellationToken);
    }

    #endregion

    #region Projection Methods Implementation

    /// <inheritdoc />
    public async Task<TResult?> GetByIdAsync<TResult>(
        TKey id,
        Expression<Func<TAggregate, TResult>> selector,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<TAggregate>()
            .AsNoTracking()
            .Where(a => a.Id.Equals(id))
            .Select(selector)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<TResult>> FindAsync<TResult>(
        Domain.ISpecification<TAggregate> specification,
        Expression<Func<TAggregate, TResult>> selector,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(specification);
        return await query.Select(selector).ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<TResult>> FindWithSelectorAsync<TResult>(
        Domain.ISpecification<TAggregate> specification,
        CancellationToken cancellationToken = default)
    {
        if (specification.Selector == null)
        {
            throw new InvalidOperationException(
                "Specification does not have a selector defined. " +
                "Use ApplySelector<TResult>() in your specification or use FindAsync<TResult> with an explicit selector parameter.");
        }

        if (specification.SelectorResultType != typeof(TResult))
        {
            throw new InvalidOperationException(
                $"Specification's selector returns {specification.SelectorResultType?.Name ?? "unknown"} " +
                $"but {typeof(TResult).Name} was requested. " +
                "Ensure the generic type parameter matches the selector's return type.");
        }

        var query = BuildQuery(specification);
        var selector = (Expression<Func<TAggregate, TResult>>)specification.Selector;
        return await query.Select(selector).ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TResult?> FirstOrDefaultAsync<TResult>(
        Domain.ISpecification<TAggregate> specification,
        Expression<Func<TAggregate, TResult>> selector,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(specification);
        return await query.Select(selector).FirstOrDefaultAsync(cancellationToken);
    }

    #endregion

    #region Paginated Results

    /// <inheritdoc />
    public async Task<IPaginate<TAggregate>> FindPagedAsync(
        Domain.ISpecification<TAggregate> specification,
        CancellationToken cancellationToken = default)
    {
        if (!specification.IsPagingEnabled)
        {
            throw new InvalidOperationException(
                "Specification must have pagination enabled. Use ApplyPagingByIndex() or ApplyPagingBySkipAndTake().");
        }

        // Build query without pagination to get total count
        var baseQuery = BuildQueryWithoutPaging(specification);
        var totalCount = await baseQuery.CountAsync(cancellationToken);

        // Build query with pagination
        var query = BuildQuery(specification);
        var items = await query.ToListAsync(cancellationToken);

        var pageIndex = specification.Skip / specification.Take;
        var pageSize = specification.Take;

        return new Paginate<TAggregate>
        {
            Index = pageIndex,
            Size = pageSize,
            Count = totalCount,
            Items = items,
            Pages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    /// <inheritdoc />
    public async Task<IPaginate<TResult>> FindPagedAsync<TResult>(
        Domain.ISpecification<TAggregate> specification,
        Expression<Func<TAggregate, TResult>> selector,
        CancellationToken cancellationToken = default)
    {
        if (!specification.IsPagingEnabled)
        {
            throw new InvalidOperationException(
                "Specification must have pagination enabled. Use ApplyPagingByIndex() or ApplyPagingBySkipAndTake().");
        }

        // Build query without pagination to get total count
        var baseQuery = BuildQueryWithoutPaging(specification);
        var totalCount = await baseQuery.CountAsync(cancellationToken);

        // Build query with pagination and projection
        var query = BuildQuery(specification);
        var items = await query.Select(selector).ToListAsync(cancellationToken);

        var pageIndex = specification.Skip / specification.Take;
        var pageSize = specification.Take;

        return new Paginate<TResult>
        {
            Index = pageIndex,
            Size = pageSize,
            Count = totalCount,
            Items = items,
            Pages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    /// <summary>
    /// Builds query without pagination for counting total items.
    /// </summary>
    private IQueryable<TAggregate> BuildQueryWithoutPaging(Domain.ISpecification<TAggregate> specification)
    {
        IQueryable<TAggregate> query = _context.Set<TAggregate>();

        if (specification.IgnoreQueryFilters)
            query = query.IgnoreQueryFilters();

        query = query.Where(specification.ToExpression());

        foreach (var criteria in specification.AdditionalCriteria)
            query = query.Where(criteria);

        if (!string.IsNullOrWhiteSpace(specification.SearchTerm) &&
            specification.SearchProperties != null &&
            specification.SearchProperties.Any())
        {
            query = ApplySearch(query, specification);
        }

        if (specification.IsDistinct)
            query = query.Distinct();

        return query;
    }

    #endregion
}
