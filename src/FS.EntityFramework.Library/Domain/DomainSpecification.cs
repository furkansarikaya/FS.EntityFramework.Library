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

    /// <summary>
    /// Gets the collection of include expressions for eager loading related entities.
    /// </summary>
    public IReadOnlyList<Expression<Func<T, object>>> Includes => _includes.AsReadOnly();

    /// <summary>
    /// Gets the collection of include strings for eager loading related entities using string-based navigation.
    /// </summary>
    public IReadOnlyList<string> IncludeStrings => _includeStrings.AsReadOnly();
    
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