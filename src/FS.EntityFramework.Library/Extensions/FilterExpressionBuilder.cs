using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using FS.EntityFramework.Library.Models;

namespace FS.EntityFramework.Library.Extensions;

/// <summary>
/// Enhanced filter expression builder with culture-safe parsing
/// Handles international number and date formats properly
/// </summary>
public static partial class FilterExpressionBuilder
{
    /// <summary>
    /// Regex pattern for validating field names: must start with a letter, followed by alphanumeric chars or dots (for navigation)
    /// </summary>
    [GeneratedRegex(@"^[a-zA-Z][a-zA-Z0-9.]*$")]
    private static partial Regex FieldNameValidationRegex();

    /// <summary>
    /// Set of valid filter operators
    /// </summary>
    private static readonly HashSet<string> ValidOperators = new(StringComparer.OrdinalIgnoreCase)
    {
        "equals", "notequals", "contains", "startswith", "endswith",
        "greaterthan", "greaterthanorequal", "lessthan", "lessthanorequal"
    };
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
        var hasFilter = false;
    
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
        // Validate field name: only alphanumeric characters and dots (for navigation properties)
        if (string.IsNullOrWhiteSpace(filterItem.Field) || !FieldNameValidationRegex().IsMatch(filterItem.Field))
            return Expression.Constant(false);

        // Validate operator against whitelist
        if (string.IsNullOrWhiteSpace(filterItem.Operator) || !ValidOperators.Contains(filterItem.Operator))
            throw new ArgumentException($"Invalid filter operator: '{filterItem.Operator}'. Valid operators: {string.Join(", ", ValidOperators)}");

        // Find the property
        var property = typeof(T).GetProperty(filterItem.Field,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

        if (property == null)
            return Expression.Constant(false); // Unknown property returns false (safe default)

        // Property expression
        var propertyExpression = Expression.Property(parameter, property);

        // FIXED: Culture-safe value conversion
        var convertedValue = ConvertValueSafely(filterItem.Value, property.PropertyType);
        var valueExpression = Expression.Constant(convertedValue, property.PropertyType);

        // Build expression based on operator type
        switch (filterItem.Operator.ToLowerInvariant())
        {
            case "equals":
                return Expression.Equal(propertyExpression, valueExpression);

            case "notequals":
                return Expression.NotEqual(propertyExpression, valueExpression);

            case "contains":
                if (property.PropertyType != typeof(string)) return Expression.Constant(false);
                var containsMethod = typeof(string).GetMethod("Contains", [typeof(string)]);
                return Expression.Call(propertyExpression, containsMethod!, valueExpression);

            case "startswith":
                if (property.PropertyType != typeof(string)) return Expression.Constant(false);
                var startsWithMethod = typeof(string).GetMethod("StartsWith", [typeof(string)]);
                return Expression.Call(propertyExpression, startsWithMethod!, valueExpression);

            case "endswith":
                if (property.PropertyType != typeof(string)) return Expression.Constant(false);
                var endsWithMethod = typeof(string).GetMethod("EndsWith", [typeof(string)]);
                return Expression.Call(propertyExpression, endsWithMethod!, valueExpression);

            case "greaterthan":
                return Expression.GreaterThan(propertyExpression, valueExpression);

            case "greaterthanorequal":
                return Expression.GreaterThanOrEqual(propertyExpression, valueExpression);

            case "lessthan":
                return Expression.LessThan(propertyExpression, valueExpression);

            case "lessthanorequal":
                return Expression.LessThanOrEqual(propertyExpression, valueExpression);

            default:
                throw new ArgumentException($"Invalid filter operator: '{filterItem.Operator}'.");
        }
    }
    
    /// <summary>
    /// FIXED: Culture-safe value conversion that handles international formats
    /// Converts a string value to the target type using invariant culture for consistency
    /// </summary>
    /// <param name="value">The string value to convert</param>
    /// <param name="targetType">The target type to convert to</param>
    /// <returns>The converted value or default value if conversion fails</returns>
    private static object? ConvertValueSafely(string value, Type targetType)
    {
        if (string.IsNullOrEmpty(value))
            return GetDefaultValue(targetType);
            
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        
        try
        {
            // CRITICAL FIX: Use InvariantCulture for consistent parsing across different locales
            // This prevents issues where "1,5" is parsed differently in TR vs EN cultures
            
            if (underlyingType == typeof(bool))
            {
                // Handle various boolean representations
                var lowerValue = value.ToLowerInvariant();
                return lowerValue is "true" or "1" or "yes" or "on" or "enabled";
            }
                
            if (underlyingType == typeof(int))
                return int.Parse(value, CultureInfo.InvariantCulture);
                
            if (underlyingType == typeof(long))
                return long.Parse(value, CultureInfo.InvariantCulture);
                
            if (underlyingType == typeof(decimal))
                return decimal.Parse(value, CultureInfo.InvariantCulture);
                
            if (underlyingType == typeof(double))
                return double.Parse(value, CultureInfo.InvariantCulture);
                
            if (underlyingType == typeof(float))
                return float.Parse(value, CultureInfo.InvariantCulture);
                
            if (underlyingType == typeof(DateTime))
            {
                // Try multiple DateTime formats for better compatibility
                if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTimeResult))
                    return dateTimeResult;

                // Fallback: try ISO 8601 format
                return DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var isoResult) ? isoResult : DateTime.MinValue;
            }
                
            if (underlyingType == typeof(DateOnly))
            {
                return DateOnly.TryParse(value, CultureInfo.InvariantCulture, out var dateOnlyResult) ? dateOnlyResult : DateOnly.MinValue;
            }
                
            if (underlyingType.IsEnum)
            {
                // Try case-insensitive enum parsing
                return Enum.TryParse(underlyingType, value, true, out var enumResult) ? enumResult : Enum.GetValues(underlyingType).GetValue(0); // Return first enum value as default
            }
                
            if (underlyingType == typeof(Guid))
            {
                return Guid.TryParse(value, out var guidResult) ? guidResult : Guid.Empty;
            }
                
            // String ve diğer tipler için - use InvariantCulture
            return Convert.ChangeType(value, underlyingType, CultureInfo.InvariantCulture);
        }
        catch (Exception)
        {
            // If all parsing attempts fail, return default value
            // Log the error in production scenarios for debugging
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