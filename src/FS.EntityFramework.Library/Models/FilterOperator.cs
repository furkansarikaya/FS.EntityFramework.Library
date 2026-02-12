namespace FS.EntityFramework.Library.Models;

/// <summary>
/// Type-safe enumeration of all supported filter operators.
/// Use with <see cref="FilterItem"/> constructor or <see cref="FilterBuilder"/> fluent API
/// for compile-time validated filtering.
/// </summary>
public enum FilterOperator
{
    /// <summary>
    /// Exact equality comparison (==). Works with all property types.
    /// </summary>
    Equals,

    /// <summary>
    /// Inequality comparison (!=). Works with all property types.
    /// </summary>
    NotEquals,

    /// <summary>
    /// Substring match (string.Contains). Works only with string properties.
    /// </summary>
    Contains,

    /// <summary>
    /// Prefix match (string.StartsWith). Works only with string properties.
    /// </summary>
    StartsWith,

    /// <summary>
    /// Suffix match (string.EndsWith). Works only with string properties.
    /// </summary>
    EndsWith,

    /// <summary>
    /// Greater than comparison (&gt;). Works with numeric, DateTime, and other comparable types.
    /// </summary>
    GreaterThan,

    /// <summary>
    /// Greater than or equal comparison (&gt;=). Works with numeric, DateTime, and other comparable types.
    /// </summary>
    GreaterThanOrEqual,

    /// <summary>
    /// Less than comparison (&lt;). Works with numeric, DateTime, and other comparable types.
    /// </summary>
    LessThan,

    /// <summary>
    /// Less than or equal comparison (&lt;=). Works with numeric, DateTime, and other comparable types.
    /// </summary>
    LessThanOrEqual,

    /// <summary>
    /// Null check (property == null). Works with nullable value types and reference types.
    /// Returns constant false for non-nullable value types. No value parameter required.
    /// </summary>
    IsNull,

    /// <summary>
    /// Not-null check (property != null). Works with nullable value types and reference types.
    /// Returns constant true for non-nullable value types. No value parameter required.
    /// </summary>
    IsNotNull,

    /// <summary>
    /// Empty string check (property == null || property == ""). Works only with string properties.
    /// Returns constant false for non-string types. No value parameter required.
    /// </summary>
    IsEmpty,

    /// <summary>
    /// Non-empty string check (property != null &amp;&amp; property != ""). Works only with string properties.
    /// Returns constant false for non-string types. No value parameter required.
    /// </summary>
    IsNotEmpty,

    /// <summary>
    /// Set membership check (WHERE property IN (...)). Value should be comma-separated.
    /// Translates to SQL IN clause via EF Core.
    /// </summary>
    In,

    /// <summary>
    /// Negated set membership check (WHERE property NOT IN (...)). Value should be comma-separated.
    /// Translates to SQL NOT IN clause via EF Core.
    /// </summary>
    NotIn
}

/// <summary>
/// Extension methods for <see cref="FilterOperator"/> enum.
/// </summary>
public static class FilterOperatorExtensions
{
    /// <summary>
    /// Converts a <see cref="FilterOperator"/> enum value to its canonical lowercase string representation
    /// used internally by <see cref="FS.EntityFramework.Library.Extensions.FilterExpressionBuilder"/>.
    /// </summary>
    /// <param name="op">The filter operator to convert.</param>
    /// <returns>The canonical lowercase string representation of the operator.</returns>
    public static string ToFilterString(this FilterOperator op) => op switch
    {
        FilterOperator.Equals => "equals",
        FilterOperator.NotEquals => "notequals",
        FilterOperator.Contains => "contains",
        FilterOperator.StartsWith => "startswith",
        FilterOperator.EndsWith => "endswith",
        FilterOperator.GreaterThan => "greaterthan",
        FilterOperator.GreaterThanOrEqual => "greaterthanorequal",
        FilterOperator.LessThan => "lessthan",
        FilterOperator.LessThanOrEqual => "lessthanorequal",
        FilterOperator.IsNull => "isnull",
        FilterOperator.IsNotNull => "isnotnull",
        FilterOperator.IsEmpty => "isempty",
        FilterOperator.IsNotEmpty => "isnotempty",
        FilterOperator.In => "in",
        FilterOperator.NotIn => "notin",
        _ => throw new ArgumentOutOfRangeException(nameof(op), op, $"Unknown filter operator: {op}")
    };
}
