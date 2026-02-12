using System.Linq.Expressions;
using System.Reflection;
using FS.EntityFramework.Library.Models;
using Microsoft.EntityFrameworkCore;

namespace FS.EntityFramework.Library.Extensions;

/// <summary>
/// Extension methods for IQueryable to provide common query operations
/// </summary>
public static class QueryableExtensions
{
    /// <summary>
    /// Applies a specification predicate to the query
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="query">The query to apply the predicate to</param>
    /// <param name="predicate">The predicate to apply</param>
    /// <returns>The query with the predicate applied</returns>
    public static IQueryable<T> ApplySpecification<T>(this IQueryable<T> query, Expression<Func<T, bool>>? predicate)
    {
        if (predicate != null)
            query = query.Where(predicate);

        return query;
    }

    /// <summary>
    /// Applies an include string to the query for loading related data
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="query">The query to apply the include to</param>
    /// <param name="includeString">The include string (e.g., "Orders.OrderItems")</param>
    /// <returns>The query with the include applied</returns>
    public static IQueryable<T> ApplyInclude<T>(this IQueryable<T> query, string? includeString) where T : class
    {
        if (!string.IsNullOrWhiteSpace(includeString))
            query = query.Include(includeString);

        return query;
    }

    /// <summary>
    /// Applies include expressions to the query for loading related data
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="query">The query to apply the includes to</param>
    /// <param name="includes">The list of include expressions</param>
    /// <returns>The query with the includes applied</returns>
    public static IQueryable<T> ApplyInclude<T>(this IQueryable<T> query, List<Expression<Func<T, object>>>? includes) where T : class
    {
        if (includes != null)
            query = includes.Aggregate(query, (current, include) => current.Include(include));

        return query;
    }

    /// <summary>
    /// Applies ordering to the query
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="query">The query to apply ordering to</param>
    /// <param name="orderBy">The ordering function</param>
    /// <returns>The query with ordering applied</returns>
    public static IQueryable<T> ApplyOrder<T>(this IQueryable<T> query,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy)
    {
        if (orderBy != null)
            query = orderBy(query);

        return query;
    }

    /// <summary>
    /// Applies pagination to the query (skip and take)
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="query">The query to apply pagination to</param>
    /// <param name="pageIndex">The page index (1-based)</param>
    /// <param name="pageSize">The number of items per page</param>
    /// <returns>The query with pagination applied</returns>
    public static IQueryable<T> ApplyPaging<T>(this IQueryable<T> query, int pageIndex, int pageSize)
    {
        return query.Skip((pageIndex - 1) * pageSize).Take(pageSize);
    }

    /// <summary>
    /// Applies dynamic filtering to the query based on the filter model.
    /// Handles SearchTerm, Filters (AND), and FilterGroups (OR/AND groups).
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="query">The query to apply filtering to</param>
    /// <param name="filter">The filter model containing search terms, criteria, and groups</param>
    /// <returns>The query with filtering applied</returns>
    public static IQueryable<T> ApplyFilter<T>(this IQueryable<T> query, FilterModel filter)
    {
        if (filter == null)
            return query;

        // SearchTerm, Filters veya FilterGroups varsa filtre ifadesi oluştur
        if (string.IsNullOrEmpty(filter.SearchTerm) && filter.Filters.Count == 0 && filter.FilterGroups.Count == 0)
            return query;
        var filterExpression = FilterExpressionBuilder.BuildFilterExpression<T>(filter);
        query = query.Where(filterExpression);

        return query;
    }

    /// <summary>
    /// Applies dynamic sorting to the query based on <see cref="FilterModel.Sorts"/>.
    /// Sort items are applied in order: the first becomes <c>OrderBy</c>,
    /// subsequent items become <c>ThenBy</c>.
    /// Invalid field names are silently skipped for safety.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="query">The query to apply sorting to</param>
    /// <param name="filter">The filter model containing sort criteria</param>
    /// <returns>The query with sorting applied, or the original query if no valid sorts exist</returns>
    public static IQueryable<T> ApplySort<T>(this IQueryable<T> query, FilterModel? filter)
    {
        if (filter?.Sorts == null || filter.Sorts.Count == 0)
            return query;

        IOrderedQueryable<T>? orderedQuery = null;

        foreach (var sort in filter.Sorts)
        {
            if (string.IsNullOrWhiteSpace(sort.Field))
                continue;

            var property = typeof(T).GetProperty(sort.Field,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            if (property == null)
                continue;

            // Build: x => x.Property
            var parameter = Expression.Parameter(typeof(T), "x");
            var propertyAccess = Expression.Property(parameter, property);
            var keySelector = Expression.Lambda(propertyAccess, parameter);

            // Select the appropriate Queryable method
            var methodName = orderedQuery == null
                ? (sort.Direction == SortDirection.Descending ? "OrderByDescending" : "OrderBy")
                : (sort.Direction == SortDirection.Descending ? "ThenByDescending" : "ThenBy");

            var method = typeof(Queryable).GetMethods()
                .First(m => m.Name == methodName && m.GetParameters().Length == 2)
                .MakeGenericMethod(typeof(T), property.PropertyType);

            orderedQuery = (IOrderedQueryable<T>)method.Invoke(null, [orderedQuery ?? (object)query, keySelector])!;
        }

        return orderedQuery ?? query;
    }
}
