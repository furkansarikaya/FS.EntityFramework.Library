using System.Linq.Expressions;

namespace FS.EntityFramework.Library.Specifications;

/// <summary>
/// Base class for specifications that define query criteria for entities.
/// </summary>
public class BaseSpecification<T>
{
    public Expression<Func<T, bool>>? Criteria { get; private set; }
    public List<Expression<Func<T, object>>> Includes { get; } = [];
    public List<string> IncludeStrings { get; } = [];
    public Expression<Func<T, object>>? OrderBy { get; private set; }
    public Expression<Func<T, object>>? OrderByDescending { get; private set; }
    public Expression<Func<T, object>>? GroupBy { get; private set; }

    public int Take { get; private set; }
    public int Skip { get; private set; }
    public bool IsPagingEnabled { get; private set; } = false;

    /// <summary>
    /// Adds a criteria expression to the specification
    /// </summary>
    /// <param name="criteria">The criteria expression to add</param>
    protected void AddCriteria(Expression<Func<T, bool>> criteria)
    {
        Criteria = criteria;
    }

    /// <summary>
    /// Adds an include expression for loading related data
    /// </summary>
    /// <param name="includeExpression">The include expression to add</param>
    protected void AddInclude(Expression<Func<T, object>> includeExpression)
    {
        Includes.Add(includeExpression);
    }

    /// <summary>
    /// Adds an include string for loading related data
    /// </summary>
    /// <param name="includeString">The include string to add</param>
    protected void AddInclude(string includeString)
    {
        IncludeStrings.Add(includeString);
    }

    /// <summary>
    /// Applies pagination to the specification
    /// </summary>
    /// <param name="skip">The number of items to skip</param>
    /// <param name="take">The number of items to take</param>
    protected void ApplyPaging(int skip, int take)
    {
        Skip = skip;
        Take = take;
        IsPagingEnabled = true;
    }

    /// <summary>
    /// Applies ascending ordering to the specification
    /// </summary>
    /// <param name="orderByExpression">The ordering expression</param>
    protected void ApplyOrderBy(Expression<Func<T, object>> orderByExpression)
    {
        OrderBy = orderByExpression;
    }

    /// <summary>
    /// Applies descending ordering to the specification
    /// </summary>
    /// <param name="orderByDescendingExpression">The ordering expression</param>
    protected void ApplyOrderByDescending(Expression<Func<T, object>> orderByDescendingExpression)
    {
        OrderByDescending = orderByDescendingExpression;
    }

    /// <summary>
    /// Applies grouping to the specification
    /// </summary>
    /// <param name="groupByExpression">The grouping expression</param>
    protected void ApplyGroupBy(Expression<Func<T, object>> groupByExpression)
    {
        GroupBy = groupByExpression;
    }
}