using System.Linq.Expressions;

namespace FS.EntityFramework.Library.Domain;

/// <summary>
/// Represents information about an include expression for eager loading.
/// Supports both simple includes and ThenInclude chains.
/// </summary>
public class IncludeExpressionInfo
{
    /// <summary>
    /// The include expression (e.g., x => x.Orders)
    /// </summary>
    public LambdaExpression Expression { get; }

    /// <summary>
    /// The type of the entity being included from
    /// </summary>
    public Type EntityType { get; }

    /// <summary>
    /// The type of the property being included
    /// </summary>
    public Type PropertyType { get; }

    /// <summary>
    /// The previous include in the chain (for ThenInclude)
    /// </summary>
    public IncludeExpressionInfo? PreviousInclude { get; }

    /// <summary>
    /// Whether this is a collection navigation (for ThenInclude behavior)
    /// </summary>
    public bool IsCollection { get; }

    public IncludeExpressionInfo(
        LambdaExpression expression,
        Type entityType,
        Type propertyType,
        IncludeExpressionInfo? previousInclude = null,
        bool isCollection = false)
    {
        Expression = expression;
        EntityType = entityType;
        PropertyType = propertyType;
        PreviousInclude = previousInclude;
        IsCollection = isCollection;
    }
}

/// <summary>
/// Specification interface for complex domain queries and business rules
/// Encapsulates query logic and makes it reusable and testable
/// </summary>
/// <typeparam name="T">The type this specification applies to</typeparam>
public interface ISpecification<T>
{
    /// <summary>
    /// Gets the selector expression for projecting entities to a different type.
    /// When set, the query will use this expression to project results instead of returning full entities.
    /// This enables efficient database-level projections where only required columns are fetched.
    /// </summary>
    /// <remarks>
    /// The selector is stored as a non-generic LambdaExpression to maintain interface compatibility.
    /// Use <see cref="SelectorResultType"/> to determine the result type.
    /// When Selector is null, the query returns full entity instances.
    /// </remarks>
    LambdaExpression? Selector { get; }

    /// <summary>
    /// Gets the result type when a selector is applied.
    /// Returns null if no selector is configured, indicating the query returns the entity type.
    /// </summary>
    Type? SelectorResultType { get; }

    /// <summary>
    /// Gets the collection of include expressions for eager loading related entities
    /// </summary>
    IReadOnlyList<Expression<Func<T, object>>> Includes { get; }

    /// <summary>
    /// Gets the collection of include expression information for advanced eager loading.
    /// Supports ThenInclude chains and filtered includes.
    /// </summary>
    IReadOnlyList<IncludeExpressionInfo> IncludeExpressions { get; }

    /// <summary>
    /// Gets the collection of include strings for eager loading related entities using string-based navigation
    /// </summary>
    IReadOnlyList<string> IncludeStrings { get; }

    /// <summary>
    /// Gets additional filter criteria expressions applied to the query.
    /// These are combined with the main ToExpression() predicate using AND logic.
    /// </summary>
    IReadOnlyList<Expression<Func<T, bool>>> AdditionalCriteria { get; }

    /// <summary>
    /// Gets the collection of ordering expressions with their direction
    /// </summary>
    IReadOnlyList<(Expression<Func<T, object>> KeySelector, bool Ascending)> OrderExpressions { get; }

    /// <summary>
    /// Gets a value indicating whether change tracking should be disabled
    /// </summary>
    bool DisableTracking { get; }

    /// <summary>
    /// Gets a value indicating whether to use identity resolution with no tracking.
    /// When true, AsNoTrackingWithIdentityResolution is used instead of AsNoTracking.
    /// </summary>
    bool UseIdentityResolution { get; }

    /// <summary>
    /// Gets a value indicating whether pagination is enabled
    /// </summary>
    bool IsPagingEnabled { get; }

    /// <summary>
    /// Gets the number of items to skip
    /// </summary>
    int Skip { get; }

    /// <summary>
    /// Gets the number of items to take
    /// </summary>
    int Take { get; }

    /// <summary>
    /// Gets the maximum number of items to return (without pagination).
    /// Used when you want to limit results without full pagination metadata.
    /// </summary>
    int? Limit { get; }

    /// <summary>
    /// Gets a value indicating whether query filters should be ignored
    /// </summary>
    bool IgnoreQueryFilters { get; }

    /// <summary>
    /// Gets a value indicating whether split query should be used
    /// </summary>
    bool AsSplitQuery { get; }

    /// <summary>
    /// Gets a value indicating whether distinct results should be returned
    /// </summary>
    bool IsDistinct { get; }

    /// <summary>
    /// Gets the grouping expression
    /// </summary>
    Expression<Func<T, object>>? GroupBy { get; }

    /// <summary>
    /// Gets the search term for text-based searching
    /// </summary>
    string? SearchTerm { get; }

    /// <summary>
    /// Gets the collection of properties to search
    /// </summary>
    IReadOnlyList<Expression<Func<T, object>>>? SearchProperties { get; }

    /// <summary>
    /// Gets the query tag for debugging and logging purposes.
    /// When set, the tag is added to the generated SQL for easier identification.
    /// </summary>
    string? QueryTag { get; }

    /// <summary>
    /// Determines whether the candidate satisfies this specification
    /// </summary>
    /// <param name="candidate">The candidate to evaluate</param>
    /// <returns>True if the candidate satisfies the specification; otherwise false</returns>
    bool IsSatisfiedBy(T candidate);

    /// <summary>
    /// Converts the specification to an expression
    /// </summary>
    /// <returns>The expression representing the specification</returns>
    Expression<Func<T, bool>> ToExpression();

    /// <summary>
    /// Combines this specification with another using logical AND
    /// </summary>
    /// <param name="other">The other specification</param>
    /// <returns>A new specification representing the AND combination</returns>
    ISpecification<T> And(ISpecification<T> other);

    /// <summary>
    /// Combines this specification with another using logical OR
    /// </summary>
    /// <param name="other">The other specification</param>
    /// <returns>A new specification representing the OR combination</returns>
    ISpecification<T> Or(ISpecification<T> other);

    /// <summary>
    /// Creates the logical negation of this specification
    /// </summary>
    /// <returns>A new specification representing the NOT of this specification</returns>
    ISpecification<T> Not();
}
