using System.Linq.Expressions;
using FS.EntityFramework.Library.Models;
using Microsoft.EntityFrameworkCore;

namespace FS.EntityFramework.Library.Extensions;

public static class QueryableExtensions
{
    public static IQueryable<T> ApplySpecification<T>(this IQueryable<T> query, Expression<Func<T, bool>>? predicate)
    {
        if (predicate != null)
            query = query.Where(predicate);
            
        return query;
    }
    
    public static IQueryable<T> ApplyInclude<T>(this IQueryable<T> query, string? includeString) where T : class
    {
        if (!string.IsNullOrWhiteSpace(includeString))
            query = query.Include(includeString);
            
        return query;
    }
    
    public static IQueryable<T> ApplyInclude<T>(this IQueryable<T> query, List<Expression<Func<T, object>>>? includes) where T : class
    {
        if (includes != null)
            query = includes.Aggregate(query, (current, include) => current.Include(include));
            
        return query;
    }
    
    public static IQueryable<T> ApplyOrder<T>(this IQueryable<T> query, 
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy)
    {
        if (orderBy != null)
            query = orderBy(query);
            
        return query;
    }
    
    public static IQueryable<T> ApplyPaging<T>(this IQueryable<T> query, int pageIndex, int pageSize)
    {
        return query.Skip((pageIndex - 1) * pageSize).Take(pageSize);
    }
    
    public static IQueryable<T> ApplyFilter<T>(this IQueryable<T> query, FilterModel filter)
    {
        if (filter == null)
            return query;
            
        // SearchTerm veya Filters varsa filtre ifadesi oluştur
        if (!string.IsNullOrEmpty(filter.SearchTerm) || filter.Filters.Any())
        {
            var filterExpression = FilterExpressionBuilder.BuildFilterExpression<T>(filter);
            query = query.Where(filterExpression);
        }
        
        return query;
    }
}