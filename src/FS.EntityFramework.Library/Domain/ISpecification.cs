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