using System.Linq.Expressions;
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
    /// Applies dynamic filtering to the query based on the filter model
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="query">The query to apply filtering to</param>
    /// <param name="filter">The filter model containing search terms and criteria</param>
    /// <returns>The query with filtering applied</returns>
    public static IQueryable<T> ApplyFilter<T>(this IQueryable<T> query, FilterModel filter)
    {
        if (filter == null)
            return query;
            
        // SearchTerm veya Filters varsa filtre ifadesi oluştur
        if (string.IsNullOrEmpty(filter.SearchTerm) && filter.Filters.Count == 0) 
            return query;
        var filterExpression = FilterExpressionBuilder.BuildFilterExpression<T>(filter);
        query = query.Where(filterExpression);

        return query;
    }
}