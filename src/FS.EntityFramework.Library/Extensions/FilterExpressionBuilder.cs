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
    /// Maps operator aliases and full names to their canonical lowercase form.
    /// Both short aliases (eq, gte) and full names (equals, greaterthanorequal) resolve to the same canonical string.
    /// </summary>
    private static readonly Dictionary<string, string> OperatorAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        // Canonical names (map to themselves)
        ["equals"] = "equals",
        ["notequals"] = "notequals",
        ["contains"] = "contains",
        ["startswith"] = "startswith",
        ["endswith"] = "endswith",
        ["greaterthan"] = "greaterthan",
        ["greaterthanorequal"] = "greaterthanorequal",
        ["lessthan"] = "lessthan",
        ["lessthanorequal"] = "lessthanorequal",
        ["isnull"] = "isnull",
        ["isnotnull"] = "isnotnull",
        ["isempty"] = "isempty",
        ["isnotempty"] = "isnotempty",
        ["in"] = "in",
        ["notin"] = "notin",

        // Short aliases
        ["eq"] = "equals",
        ["neq"] = "notequals",
        ["gt"] = "greaterthan",
        ["gte"] = "greaterthanorequal",
        ["lt"] = "lessthan",
        ["lte"] = "lessthanorequal",
        ["sw"] = "startswith",
        ["ew"] = "endswith"
    };

    /// <summary>
    /// Set of operators that do not require a value parameter.
    /// </summary>
    private static readonly HashSet<string> ValuelessOperators = new(StringComparer.OrdinalIgnoreCase)
    {
        "isnull", "isnotnull", "isempty", "isnotempty"
    };

    /// <summary>
    /// Set of operators that work with comma-separated value lists.
    /// </summary>
    private static readonly HashSet<string> SetOperators = new(StringComparer.OrdinalIgnoreCase)
    {
        "in", "notin"
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

        // Özel filtreler (AND logic)
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

        // Filter groups (OR/AND groups combined with AND between them)
        foreach (var group in filter.FilterGroups)
        {
            if (group.Filters.Count == 0) continue;

            var groupExpression = BuildFilterGroupExpression<T>(parameter, group);

            if (hasFilter)
            {
                body = Expression.AndAlso(body, groupExpression);
            }
            else
            {
                body = groupExpression;
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
    /// Builds a combined expression for a <see cref="FilterGroup"/> by combining its filters
    /// using the group's <see cref="FilterLogic"/> (AND or OR).
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="parameter">The parameter expression for the entity</param>
    /// <param name="group">The filter group containing filters and logic operator</param>
    /// <returns>A combined expression for the group</returns>
    private static Expression BuildFilterGroupExpression<T>(ParameterExpression parameter, FilterGroup group)
    {
        Expression? combined = null;

        foreach (var filterItem in group.Filters)
        {
            var itemExpression = BuildFilterItemExpression<T>(parameter, filterItem);

            if (combined == null)
            {
                combined = itemExpression;
            }
            else
            {
                combined = group.Logic == FilterLogic.Or
                    ? Expression.OrElse(combined, itemExpression)
                    : Expression.AndAlso(combined, itemExpression);
            }
        }

        return combined ?? Expression.Constant(true);
    }

    /// <summary>
    /// Builds a filter expression for a specific field and value using the specified operator.
    /// Supports operator aliases, value-less operators, and set operators (in/notin).
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

        // Resolve operator alias to canonical form
        if (string.IsNullOrWhiteSpace(filterItem.Operator) ||
            !OperatorAliases.TryGetValue(filterItem.Operator, out var canonicalOperator))
        {
            throw new ArgumentException(
                $"Invalid filter operator: '{filterItem.Operator}'. Valid operators: {string.Join(", ", OperatorAliases.Keys)}");
        }

        // Find the property
        var property = typeof(T).GetProperty(filterItem.Field,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

        if (property == null)
            return Expression.Constant(false); // Unknown property returns false (safe default)

        // Property expression
        var propertyExpression = Expression.Property(parameter, property);

        // Value-less operators (isnull, isnotnull, isempty, isnotempty)
        if (ValuelessOperators.Contains(canonicalOperator))
            return BuildValuelessExpression(propertyExpression, property.PropertyType, canonicalOperator);

        // Set operators (in, notin)
        if (SetOperators.Contains(canonicalOperator))
            return BuildInExpression(propertyExpression, property.PropertyType, filterItem.Value, canonicalOperator);

        // Standard operators require value conversion
        var convertedValue = ConvertValueSafely(filterItem.Value ?? string.Empty, property.PropertyType);
        var valueExpression = Expression.Constant(convertedValue, property.PropertyType);

        // Build expression based on canonical operator
        return canonicalOperator switch
        {
            "equals" => Expression.Equal(propertyExpression, valueExpression),
            "notequals" => Expression.NotEqual(propertyExpression, valueExpression),
            "contains" => BuildStringMethodExpression(property, propertyExpression, valueExpression, "Contains"),
            "startswith" => BuildStringMethodExpression(property, propertyExpression, valueExpression, "StartsWith"),
            "endswith" => BuildStringMethodExpression(property, propertyExpression, valueExpression, "EndsWith"),
            "greaterthan" => Expression.GreaterThan(propertyExpression, valueExpression),
            "greaterthanorequal" => Expression.GreaterThanOrEqual(propertyExpression, valueExpression),
            "lessthan" => Expression.LessThan(propertyExpression, valueExpression),
            "lessthanorequal" => Expression.LessThanOrEqual(propertyExpression, valueExpression),
            _ => throw new ArgumentException($"Invalid filter operator: '{filterItem.Operator}'.")
        };
    }

    /// <summary>
    /// Builds a string method call expression (Contains, StartsWith, EndsWith).
    /// Returns constant false for non-string properties.
    /// </summary>
    private static Expression BuildStringMethodExpression(
        PropertyInfo property,
        MemberExpression propertyExpression,
        ConstantExpression valueExpression,
        string methodName)
    {
        if (property.PropertyType != typeof(string))
            return Expression.Constant(false);

        var method = typeof(string).GetMethod(methodName, [typeof(string)]);
        return Expression.Call(propertyExpression, method!, valueExpression);
    }

    /// <summary>
    /// Builds an expression for value-less operators: isnull, isnotnull, isempty, isnotempty.
    /// Handles nullable types, reference types, and non-nullable value types correctly.
    /// </summary>
    /// <param name="propertyExpression">The property access expression.</param>
    /// <param name="propertyType">The CLR type of the property.</param>
    /// <param name="canonicalOperator">The canonical operator string (lowercase).</param>
    /// <returns>The boolean expression for the value-less check.</returns>
    private static Expression BuildValuelessExpression(
        MemberExpression propertyExpression,
        Type propertyType,
        string canonicalOperator)
    {
        var isNullableOrReference = !propertyType.IsValueType || Nullable.GetUnderlyingType(propertyType) != null;

        switch (canonicalOperator)
        {
            case "isnull":
                return isNullableOrReference
                    ? Expression.Equal(propertyExpression, Expression.Constant(null, propertyType))
                    : Expression.Constant(false);

            case "isnotnull":
                return isNullableOrReference
                    ? Expression.NotEqual(propertyExpression, Expression.Constant(null, propertyType))
                    : Expression.Constant(true);

            case "isempty":
                if (propertyType != typeof(string))
                    return Expression.Constant(false);
                // x.Prop == null || x.Prop == ""
                var isNullExpr = Expression.Equal(propertyExpression, Expression.Constant(null, typeof(string)));
                var isEmptyExpr = Expression.Equal(propertyExpression, Expression.Constant(string.Empty));
                return Expression.OrElse(isNullExpr, isEmptyExpr);

            case "isnotempty":
                if (propertyType != typeof(string))
                    return Expression.Constant(false);
                // x.Prop != null && x.Prop != ""
                var notNullExpr = Expression.NotEqual(propertyExpression, Expression.Constant(null, typeof(string)));
                var notEmptyExpr = Expression.NotEqual(propertyExpression, Expression.Constant(string.Empty));
                return Expression.AndAlso(notNullExpr, notEmptyExpr);

            default:
                return Expression.Constant(false);
        }
    }

    /// <summary>
    /// Builds an IN or NOT IN expression from a comma-separated value string.
    /// Parses values, converts each to the target property type, and generates a
    /// <c>list.Contains(x.Prop)</c> expression that EF Core translates to <c>WHERE Prop IN (...)</c>.
    /// </summary>
    /// <param name="propertyExpression">The property access expression.</param>
    /// <param name="propertyType">The CLR type of the property.</param>
    /// <param name="value">Comma-separated values (e.g., "1,2,3").</param>
    /// <param name="canonicalOperator">"in" or "notin".</param>
    /// <returns>The boolean expression for the set membership check.</returns>
    private static Expression BuildInExpression(
        MemberExpression propertyExpression,
        Type propertyType,
        string? value,
        string canonicalOperator)
    {
        // Parse comma-separated values
        var rawValues = (value ?? string.Empty)
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        // Empty set semantics: IN () -> false, NOT IN () -> true
        if (rawValues.Length == 0)
            return Expression.Constant(canonicalOperator == "notin");

        // Build a typed list and convert each value
        var listType = typeof(List<>).MakeGenericType(propertyType);
        var list = Activator.CreateInstance(listType)!;
        var addMethod = listType.GetMethod("Add")!;

        foreach (var rawValue in rawValues)
        {
            var converted = ConvertValueSafely(rawValue, propertyType);
            addMethod.Invoke(list, [converted]);
        }

        // list.Contains(x.Prop)
        var listExpression = Expression.Constant(list, listType);
        var containsMethod = listType.GetMethod("Contains", [propertyType])!;
        var containsCall = Expression.Call(listExpression, containsMethod, propertyExpression);

        // NOT IN -> negate
        return canonicalOperator == "notin"
            ? Expression.Not(containsCall)
            : containsCall;
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
