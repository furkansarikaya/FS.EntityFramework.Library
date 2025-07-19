using System.Linq.Expressions;

namespace FS.EntityFramework.Library.Domain;

/// <summary>
/// Base implementation of the specification pattern for domain logic
/// Provides composition methods for building complex specifications
/// </summary>
/// <typeparam name="T">The type this specification applies to</typeparam>
public abstract class DomainSpecification<T> : ISpecification<T>
{
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