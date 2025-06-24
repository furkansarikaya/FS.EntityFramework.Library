using System.Linq.Expressions;
using System.Reflection;
using FS.EntityFramework.Library.Models;

namespace FS.EntityFramework.Library.Extensions;

/// <summary>
/// Dinamik filtre ifadeleri oluşturmak için yardımcı sınıf
/// </summary>
public static class FilterExpressionBuilder
{
    /// <summary>
    /// Builds a LINQ expression predicate from a filter model for dynamic querying
    /// </summary>
    /// <typeparam name="T">The entity type to build the filter for</typeparam>
    /// <param name="filter">The filter model containing search terms and filter criteria</param>
    /// <returns>A compiled expression that can be used in LINQ queries</returns>
    public static Expression<Func<T, bool>> BuildFilterExpression<T>(FilterModel filter)
    {
        // Parametre ifadesi oluştur (x => ...)
        var parameter = Expression.Parameter(typeof(T), "x");
    
        // Başlangıç olarak "true" ifadesini kullan
        Expression body = Expression.Constant(true);
        bool hasFilter = false;
    
        // SearchTerm kontrolü
        if (!string.IsNullOrEmpty(filter.SearchTerm))
        {
            var searchExpression = BuildSearchTermExpression<T>(parameter, filter.SearchTerm);
            body = searchExpression;
            hasFilter = true;
        }
    
        // Özel filtreler
        foreach (var filterExpression in filter.Filters.Select(filterItem => BuildFilterItemExpression<T>(parameter, filterItem)))
        {
            if (hasFilter)
            {
                body = Expression.AndAlso(body, filterExpression);
            }
            else
            {
                body = filterExpression;
                hasFilter = true;
            }
        }
    
        // Lambda ifadesi oluştur
        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }
    
    /// <summary>
    /// Builds a search expression that searches across all string properties of the entity
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="parameter">The parameter expression for the entity</param>
    /// <param name="searchTerm">The search term to look for</param>
    /// <returns>An expression that performs a contains search across string properties</returns>
    private static Expression BuildSearchTermExpression<T>(ParameterExpression parameter, string searchTerm)
    {
        // String özellikleri bul
        var stringProperties = typeof(T).GetProperties()
            .Where(p => p.PropertyType == typeof(string))
            .ToList();
            
        if (stringProperties.Count == 0)
            return Expression.Constant(true); // Hiç string özellik yoksa true döndür
            
        // Her string özellik için Contains ifadesi oluştur ve OR ile birleştir
        Expression? combinedExpression = null;
        
        foreach (var prop in stringProperties)
        {
            var property = Expression.Property(parameter, prop);
            var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });
            var searchValue = Expression.Constant(searchTerm);
            
            // null kontrolü ekle (prop != null && prop.Contains(searchTerm))
            var nullCheck = Expression.NotEqual(property, Expression.Constant(null, typeof(string)));
            var containsExpression = Expression.Call(property, containsMethod!, searchValue);
            var safeContainsExpression = Expression.AndAlso(nullCheck, containsExpression);
            
            combinedExpression = combinedExpression == null
                ? safeContainsExpression
                : Expression.OrElse(combinedExpression, safeContainsExpression);
        }
        
        return combinedExpression ?? Expression.Constant(true);
    }
    
    /// <summary>
    /// Builds a filter expression for a specific field and value using the specified operator
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="parameter">The parameter expression for the entity</param>
    /// <param name="filterItem">The filter item containing field, operator, and value</param>
    /// <returns>An expression that applies the specified filter</returns>
    private static Expression BuildFilterItemExpression<T>(ParameterExpression parameter, FilterItem filterItem)
    {
        // Özelliği bul
        var property = typeof(T).GetProperty(filterItem.Field, 
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            
        if (property == null)
            return Expression.Constant(true); // Özellik bulunamazsa true döndür
            
        // Özellik ifadesi
        var propertyExpression = Expression.Property(parameter, property);
        
        // Değeri uygun tipe dönüştür
        object? convertedValue = ConvertValue(filterItem.Value, property.PropertyType);
        var valueExpression = Expression.Constant(convertedValue, property.PropertyType);
        
        // Operatör tipine göre ifade oluştur
        switch (filterItem.Operator.ToLower())
        {
            case "equals":
                return Expression.Equal(propertyExpression, valueExpression);
                
            case "notequals":
                return Expression.NotEqual(propertyExpression, valueExpression);
                
            case "contains":
                if (property.PropertyType == typeof(string))
                {
                    var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });
                    return Expression.Call(propertyExpression, containsMethod!, valueExpression);
                }
                return Expression.Constant(true);
                
            case "startswith":
                if (property.PropertyType == typeof(string))
                {
                    var startsWithMethod = typeof(string).GetMethod("StartsWith", new[] { typeof(string) });
                    return Expression.Call(propertyExpression, startsWithMethod!, valueExpression);
                }
                return Expression.Constant(true);
                
            case "endswith":
                if (property.PropertyType == typeof(string))
                {
                    var endsWithMethod = typeof(string).GetMethod("EndsWith", new[] { typeof(string) });
                    return Expression.Call(propertyExpression, endsWithMethod!, valueExpression);
                }
                return Expression.Constant(true);
                
            case "greaterthan":
                return Expression.GreaterThan(propertyExpression, valueExpression);
                
            case "greaterthanorequal":
                return Expression.GreaterThanOrEqual(propertyExpression, valueExpression);
                
            case "lessthan":
                return Expression.LessThan(propertyExpression, valueExpression);
                
            case "lessthanorequal":
                return Expression.LessThanOrEqual(propertyExpression, valueExpression);
                
            default:
                return Expression.Constant(true);
        }
    }
    
    /// <summary>
    /// Converts a string value to the target type for use in filter expressions
    /// </summary>
    /// <param name="value">The string value to convert</param>
    /// <param name="targetType">The target type to convert to</param>
    /// <returns>The converted value or default value if conversion fails</returns>
    private static object? ConvertValue(string value, Type targetType)
    {
        if (string.IsNullOrEmpty(value))
            return GetDefaultValue(targetType);
            
        // Nullable tiplerle başa çık
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        
        try
        {
            if (underlyingType == typeof(bool))
                return bool.Parse(value);
                
            if (underlyingType == typeof(int))
                return int.Parse(value);
                
            if (underlyingType == typeof(decimal))
                return decimal.Parse(value);
                
            if (underlyingType == typeof(DateTime))
                return DateTime.Parse(value);
                
            if (underlyingType.IsEnum)
                return Enum.Parse(underlyingType, value);
                
            // String ve diğer tipler için
            return Convert.ChangeType(value, underlyingType);
        }
        catch
        {
            return GetDefaultValue(targetType);
        }
    }
    
    /// <summary>
    /// Gets the default value for the specified type
    /// </summary>
    /// <param name="t">The type to get the default value for</param>
    /// <returns>The default value for the type</returns>
    private static object? GetDefaultValue(Type t)
    {
        return t.IsValueType ? Activator.CreateInstance(t) : null;
    }
}