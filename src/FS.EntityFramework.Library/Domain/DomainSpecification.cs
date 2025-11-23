using System.Linq.Expressions;

namespace FS.EntityFramework.Library.Domain;

/// <summary>
/// Base implementation of the specification pattern for domain logic
/// Provides composition methods for building complex specifications
/// </summary>
/// <typeparam name="T">The type this specification applies to</typeparam>
public abstract class DomainSpecification<T> : ISpecification<T>
{
    private readonly List<Expression<Func<T, object>>> _includes = [];
    private readonly List<string> _includeStrings = [];
    private readonly List<(Expression<Func<T, object>> KeySelector, bool Ascending)> _orderExpressions = [];


    /// <summary>
    /// Gets the collection of include expressions for eager loading related entities.
    /// </summary>
    public IReadOnlyList<Expression<Func<T, object>>> Includes => _includes.AsReadOnly();

    /// <summary>
    /// Gets the collection of include strings for eager loading related entities using string-based navigation.
    /// </summary>
    public IReadOnlyList<string> IncludeStrings => _includeStrings.AsReadOnly();
    
    /// <summary>
    /// Gets the collection of ordering expressions with their direction.
    /// </summary>
    public IReadOnlyList<(Expression<Func<T, object>> KeySelector, bool Ascending)> OrderExpressions => _orderExpressions.AsReadOnly();
    
    /// <summary>
    /// Gets a value indicating whether change tracking should be disabled for this specification.
    /// Disabling tracking improves performance for read-only queries.
    /// </summary>
    public bool DisableTracking { get; private set; } = true;

    /// <summary>
    /// Gets a value indicating whether pagination is enabled for this specification.
    /// </summary>
    public bool IsPagingEnabled { get; private set; }

    /// <summary>
    /// Gets the number of items to skip when pagination is enabled.
    /// Used in conjunction with Take for implementing pagination.
    /// </summary>
    public int Skip { get; private set; }

    /// <summary>
    /// Gets the number of items to take when pagination is enabled.
    /// Represents the page size in pagination scenarios.
    /// </summary>
    public int Take { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the specification should ignore query filters (e.g., soft delete filters).
    /// </summary>
    public bool IgnoreQueryFilters { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the query should be split into multiple queries.
    /// Useful for complex includes that might cause cartesian explosion.
    /// </summary>
    public bool AsSplitQuery { get; private set; }

    /// <summary>
    /// Gets the grouping expression for aggregating results.
    /// </summary>
    public Expression<Func<T, object>>? GroupBy { get; private set; }

    /// <summary>
    /// Gets the search term for text-based searching across multiple properties.
    /// </summary>
    public string? SearchTerm { get; private set; }

    /// <summary>
    /// Gets the collection of properties to search when SearchTerm is specified.
    /// </summary>
    public IReadOnlyList<Expression<Func<T, object>>>? SearchProperties { get; private set; }
    
    /// <summary>
    /// When implemented in a derived class, determines whether the candidate satisfies this specification
    /// </summary>
    /// <param name="candidate">The candidate to evaluate</param>
    /// <returns>True if the candidate satisfies the specification; otherwise false</returns>
    public abstract bool IsSatisfiedBy(T candidate);

    /// <summary>
    /// Converts this specification to a predicate function
    /// </summary>
    /// <returns>A predicate function</returns>
    public abstract Expression<Func<T, bool>> ToExpression();

    /// <summary>
    /// Adds an include expression for eager loading a related entity or collection.
    /// This method enables loading of navigation properties to avoid N+1 query problems.
    /// </summary>
    /// <param name="includeExpression">Expression pointing to the navigation property to include</param>
    /// <returns>The specification instance for method chaining</returns>
    /// <example>
    /// <code>
    /// // Include single navigation property
    /// AddInclude(order => order.Customer);
    /// 
    /// // Include collection navigation property
    /// AddInclude(order => order.OrderItems);
    /// </code>
    /// </example>
    protected DomainSpecification<T> AddInclude(Expression<Func<T, object>> includeExpression)
    {
        ArgumentNullException.ThrowIfNull(includeExpression);

        _includes.Add(includeExpression);
        return this;
    }

    /// <summary>
    /// Adds a string-based include for eager loading related entities using navigation property path.
    /// This method is useful for including nested properties or when working with dynamic scenarios.
    /// </summary>
    /// <param name="includeString">The navigation property path as a string (e.g., "Customer.Address")</param>
    /// <returns>The specification instance for method chaining</returns>
    /// <example>
    /// <code>
    /// // Include single level
    /// AddInclude("Customer");
    /// 
    /// // Include nested properties (ThenInclude equivalent)
    /// AddInclude("OrderItems.Product");
    /// AddInclude("OrderItems.Product.Category");
    /// </code>
    /// </example>
    protected DomainSpecification<T> AddInclude(string includeString)
    {
        if (string.IsNullOrWhiteSpace(includeString))
            throw new ArgumentException("Include string cannot be null or whitespace", nameof(includeString));

        _includeStrings.Add(includeString);
        return this;
    }

    /// <summary>
    /// Adds multiple include expressions for eager loading multiple related entities.
    /// This is a convenience method for adding multiple includes in one call.
    /// </summary>
    /// <param name="includeExpressions">Collection of expressions pointing to navigation properties</param>
    /// <returns>The specification instance for method chaining</returns>
    /// <example>
    /// <code>
    /// AddIncludes(
    ///     order => order.Customer,
    ///     order => order.OrderItems,
    ///     order => order.ShippingAddress
    /// );
    /// </code>
    /// </example>
    protected DomainSpecification<T> AddIncludes(params Expression<Func<T, object>>[] includeExpressions)
    {
        ArgumentNullException.ThrowIfNull(includeExpressions);

        foreach (var includeExpression in includeExpressions)
        {
            if (includeExpression != null)
                _includes.Add(includeExpression);
        }

        return this;
    }

    /// <summary>
    /// Adds multiple string-based includes for eager loading multiple related entities.
    /// This is a convenience method for adding multiple string includes in one call.
    /// </summary>
    /// <param name="includeStrings">Collection of navigation property paths as strings</param>
    /// <returns>The specification instance for method chaining</returns>
    /// <example>
    /// <code>
    /// AddIncludes(
    ///     "Customer",
    ///     "OrderItems.Product",
    ///     "ShippingAddress.City"
    /// );
    /// </code>
    /// </example>
    protected DomainSpecification<T> AddIncludes(params string[] includeStrings)
    {
        ArgumentNullException.ThrowIfNull(includeStrings);

        foreach (var includeString in includeStrings)
        {
            if (!string.IsNullOrWhiteSpace(includeString))
                _includeStrings.Add(includeString);
        }

        return this;
    }

    // ===== ORDERING METHODS =====

    /// <summary>
    /// Adds an ascending order expression to the specification.
    /// Multiple order expressions are applied in the order they are added (ThenBy behavior).
    /// </summary>
    /// <param name="orderExpression">Expression defining the property to order by</param>
    /// <returns>The specification instance for method chaining</returns>
    /// <example>
    /// <code>
    /// AddOrderBy(product => product.Category)
    ///     .AddOrderBy(product => product.Price);
    /// // Results in: ORDER BY Category ASC, Price ASC
    /// </code>
    /// </example>
    protected DomainSpecification<T> AddOrderBy(Expression<Func<T, object>> orderExpression)
    {
        ArgumentNullException.ThrowIfNull(orderExpression);

        _orderExpressions.Add((orderExpression, true));
        return this;
    }

    /// <summary>
    /// Adds a descending order expression to the specification.
    /// Multiple order expressions are applied in the order they are added (ThenByDescending behavior).
    /// </summary>
    /// <param name="orderExpression">Expression defining the property to order by</param>
    /// <returns>The specification instance for method chaining</returns>
    /// <example>
    /// <code>
    /// AddOrderByDescending(product => product.CreatedAt)
    ///     .AddOrderBy(product => product.Name);
    /// // Results in: ORDER BY CreatedAt DESC, Name ASC
    /// </code>
    /// </example>
    protected DomainSpecification<T> AddOrderByDescending(Expression<Func<T, object>> orderExpression)
    {
        ArgumentNullException.ThrowIfNull(orderExpression);

        _orderExpressions.Add((orderExpression, false));
        return this;
    }

    // ===== PAGINATION METHODS =====

    /// <summary>
    /// Enables pagination for this specification with the specified page index and page size.
    /// Uses 0-based page indexing (first page is 0).
    /// </summary>
    /// <param name="pageIndex">The zero-based page index</param>
    /// <param name="pageSize">The number of items per page</param>
    /// <returns>The specification instance for method chaining</returns>
    /// <example>
    /// <code>
    /// ApplyPaging(0, 20); // First page, 20 items
    /// ApplyPaging(2, 10); // Third page, 10 items
    /// </code>
    /// </example>
    protected DomainSpecification<T> ApplyPagingByIndex(int pageIndex, int pageSize)
    {
        if (pageIndex < 0)
            throw new ArgumentException("Page index must be non-negative", nameof(pageIndex));
        
        if (pageSize <= 0)
            throw new ArgumentException("Page size must be positive", nameof(pageSize));

        Skip = pageIndex * pageSize;
        Take = pageSize;
        IsPagingEnabled = true;
        return this;
    }

    /// <summary>
    /// Applies custom skip and take values for advanced pagination scenarios.
    /// </summary>
    /// <param name="skip">The number of items to skip</param>
    /// <param name="take">The number of items to take</param>
    /// <returns>The specification instance for method chaining</returns>
    protected DomainSpecification<T> ApplyPagingBySkipAndTake(int skip, int take)
    {
        if (skip < 0)
            throw new ArgumentException("Skip must be non-negative", nameof(skip));
        
        if (take <= 0)
            throw new ArgumentException("Take must be positive", nameof(take));

        Skip = skip;
        Take = take;
        IsPagingEnabled = true;
        return this;
    }

    // ===== TRACKING CONTROL =====

    /// <summary>
    /// Enables change tracking for this specification.
    /// Use this when you need to modify the entities after loading them.
    /// </summary>
    /// <returns>The specification instance for method chaining</returns>
    protected DomainSpecification<T> EnableTracking()
    {
        DisableTracking = false;
        return this;
    }

    /// <summary>
    /// Disables change tracking for this specification (default behavior).
    /// Improves performance for read-only queries.
    /// </summary>
    /// <returns>The specification instance for method chaining</returns>
    protected DomainSpecification<T> AsNoTracking()
    {
        DisableTracking = true;
        return this;
    }

    // ===== QUERY FILTER CONTROL =====

    /// <summary>
    /// Configures this specification to ignore global query filters (e.g., soft delete filters).
    /// Use this when you need to access all entities including filtered ones.
    /// </summary>
    /// <returns>The specification instance for method chaining</returns>
    /// <example>
    /// <code>
    /// // Include soft-deleted entities
    /// ApplyIgnoreQueryFilters();
    /// </code>
    /// </example>
    protected DomainSpecification<T> ApplyIgnoreQueryFilters()
    {
        IgnoreQueryFilters = true;
        return this;
    }

    // ===== SPLIT QUERY =====

    /// <summary>
    /// Configures this specification to use split queries for complex includes.
    /// Prevents cartesian explosion when loading multiple collections.
    /// </summary>
    /// <returns>The specification instance for method chaining</returns>
    /// <example>
    /// <code>
    /// // Use split query when including multiple collections
    /// AddInclude(order => order.OrderItems)
    ///     .AddInclude(order => order.Payments)
    ///     .EnableSplitQuery();
    /// </code>
    /// </example>
    protected DomainSpecification<T> EnableSplitQuery()
    {
        AsSplitQuery = true;
        return this;
    }

    // ===== GROUPING =====

    /// <summary>
    /// Applies a grouping expression to the specification for aggregation scenarios.
    /// </summary>
    /// <param name="groupExpression">Expression defining the property to group by</param>
    /// <returns>The specification instance for method chaining</returns>
    /// <example>
    /// <code>
    /// ApplyGroupBy(product => product.CategoryId);
    /// </code>
    /// </example>
    protected DomainSpecification<T> ApplyGroupBy(Expression<Func<T, object>> groupExpression)
    {
        ArgumentNullException.ThrowIfNull(groupExpression);

        GroupBy = groupExpression;
        return this;
    }

    // ===== SEARCH =====

    /// <summary>
    /// Configures text-based search across specified properties.
    /// The search is case-insensitive and uses Contains logic.
    /// </summary>
    /// <param name="searchTerm">The term to search for</param>
    /// <param name="searchProperties">Properties to search in</param>
    /// <returns>The specification instance for method chaining</returns>
    /// <example>
    /// <code>
    /// ApplySearch("laptop", 
    ///     p => p.Name, 
    ///     p => p.Description, 
    ///     p => p.Brand);
    /// </code>
    /// </example>
    protected DomainSpecification<T> ApplySearch(string searchTerm, params Expression<Func<T, object>>[] searchProperties)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return this;

        if (searchProperties == null || searchProperties.Length == 0)
            throw new ArgumentException("At least one search property must be specified", nameof(searchProperties));

        SearchTerm = searchTerm;
        SearchProperties = searchProperties.ToList().AsReadOnly();
        return this;
    }

    /// <summary>
    /// Combines this specification with another using logical AND
    /// </summary>
    /// <param name="other">The other specification</param>
    /// <returns>A new specification representing the AND combination</returns>
    public ISpecification<T> And(ISpecification<T> other)
    {
        return new AndSpecification<T>(this, other);
    }

    /// <summary>
    /// Combines this specification with another using logical OR
    /// </summary>
    /// <param name="other">The other specification</param>
    /// <returns>A new specification representing the OR combination</returns>
    public ISpecification<T> Or(ISpecification<T> other)
    {
        return new OrSpecification<T>(this, other);
    }

    /// <summary>
    /// Creates the logical negation of this specification
    /// </summary>
    /// <returns>A new specification representing the NOT of this specification</returns>
    public ISpecification<T> Not()
    {
        return new NotSpecification<T>(this);
    }

    /// <summary>
    /// Implicitly converts a specification to a predicate function
    /// </summary>
    /// <param name="specification">The specification to convert</param>
    /// <returns>A predicate function</returns>
    public static implicit operator Func<T, bool>(DomainSpecification<T> specification)
    {
        return specification.IsSatisfiedBy;
    }
}

/// <summary>
/// Internal specification implementing logical AND operation
/// </summary>
/// <typeparam name="T">The type this specification applies to</typeparam>
internal class AndSpecification<T>(ISpecification<T> left, ISpecification<T> right) : DomainSpecification<T>
{
    public override bool IsSatisfiedBy(T candidate)
    {
        return left.IsSatisfiedBy(candidate) && right.IsSatisfiedBy(candidate);
    }
    
    public override Expression<Func<T, bool>> ToExpression()
    {
        var leftExpr = left.ToExpression();
        var rightExpr = right.ToExpression();

        var param = Expression.Parameter(typeof(T));
        var body = Expression.AndAlso(
            Expression.Invoke(leftExpr, param),
            Expression.Invoke(rightExpr, param)
        );

        return Expression.Lambda<Func<T, bool>>(body, param);
    }
}

/// <summary>
/// Internal specification implementing logical OR operation
/// </summary>
/// <typeparam name="T">The type this specification applies to</typeparam>
internal class OrSpecification<T>(ISpecification<T> left, ISpecification<T> right) : DomainSpecification<T>
{
    public override bool IsSatisfiedBy(T candidate)
    {
        return left.IsSatisfiedBy(candidate) || right.IsSatisfiedBy(candidate);
    }

    public override Expression<Func<T, bool>> ToExpression()
    {
        var leftExpr = left.ToExpression();
        var rightExpr = right.ToExpression();

        var param = Expression.Parameter(typeof(T));
        var body = Expression.OrElse(
            Expression.Invoke(leftExpr, param),
            Expression.Invoke(rightExpr, param)
        );

        return Expression.Lambda<Func<T, bool>>(body, param);
    }
}

/// <summary>
/// Internal specification implementing logical NOT operation
/// </summary>
/// <typeparam name="T">The type this specification applies to</typeparam>
internal class NotSpecification<T>(ISpecification<T> specification) : DomainSpecification<T>
{
    public override bool IsSatisfiedBy(T candidate)
    {
        return !specification.IsSatisfiedBy(candidate);
    }
    
    public override Expression<Func<T, bool>> ToExpression()
    {
        var expr = specification.ToExpression();
        var param = expr.Parameters[0];
        var body = Expression.Not(expr.Body);
        return Expression.Lambda<Func<T, bool>>(body, param);
    }
}