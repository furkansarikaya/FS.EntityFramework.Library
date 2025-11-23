using System.Linq.Expressions;

namespace FS.EntityFramework.Library.Domain;

/// <summary>
/// Specification interface for complex domain queries and business rules
/// Encapsulates query logic and makes it reusable and testable
/// </summary>
/// <typeparam name="T">The type this specification applies to</typeparam>
public interface ISpecification<T>
{
    /// <summary>
    /// Gets the collection of include expressions for eager loading related entities
    /// </summary>
    IReadOnlyList<Expression<Func<T, object>>> Includes { get; }

    /// <summary>
    /// Gets the collection of include strings for eager loading related entities using string-based navigation
    /// </summary>
    IReadOnlyList<string> IncludeStrings { get; }
    
    /// <summary>
    /// Gets the collection of ordering expressions with their direction
    /// </summary>
    IReadOnlyList<(Expression<Func<T, object>> KeySelector, bool Ascending)> OrderExpressions { get; }

    /// <summary>
    /// Gets a value indicating whether change tracking should be disabled
    /// </summary>
    bool DisableTracking { get; }

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
    /// Gets a value indicating whether query filters should be ignored
    /// </summary>
    bool IgnoreQueryFilters { get; }

    /// <summary>
    /// Gets a value indicating whether split query should be used
    /// </summary>
    bool AsSplitQuery { get; }

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