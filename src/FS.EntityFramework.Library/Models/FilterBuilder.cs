using System.Globalization;
using System.Linq.Expressions;

namespace FS.EntityFramework.Library.Models;

#region Expression Helper

/// <summary>
/// Internal utility for extracting property paths from lambda expressions.
/// Used by <see cref="FilterBuilder{T}"/> and <see cref="FilterGroupBuilder{T}"/>.
/// </summary>
internal static class FilterExpressionHelper
{
    /// <summary>
    /// Extracts the property path from a lambda expression (e.g., <c>p => p.Category.Name</c> → <c>"Category.Name"</c>).
    /// Handles unboxing (Convert) expressions for value types.
    /// </summary>
    internal static string ExtractPropertyPath<T>(Expression<Func<T, object?>> expression)
    {
        var body = expression.Body;

        // Unwrap Convert (boxing for value types: p => (object)p.Price)
        if (body is UnaryExpression { NodeType: ExpressionType.Convert } unary)
            body = unary.Operand;

        var parts = new List<string>();
        var current = body;
        while (current is MemberExpression member)
        {
            parts.Insert(0, member.Member.Name);
            current = member.Expression;
        }

        if (parts.Count == 0)
            throw new ArgumentException(
                $"Expression '{expression}' does not refer to a property. Expected a member expression like p => p.PropertyName.");

        return string.Join(".", parts);
    }

    /// <summary>
    /// Converts a value to its invariant culture string representation for use in <see cref="FilterItem.Value"/>.
    /// Returns null for null values. Uses <see cref="IFormattable"/> when available for culture-safe formatting.
    /// </summary>
    internal static string? ConvertToFilterString(object? value)
    {
        if (value is null)
            return null;

        if (value is string s)
            return s;

        if (value is IFormattable formattable)
            return formattable.ToString(null, CultureInfo.InvariantCulture);

        return value.ToString();
    }

    /// <summary>
    /// Converts multiple values to a comma-separated invariant string for In/NotIn operators.
    /// </summary>
    internal static string ConvertToCommaSeparated(IEnumerable<object?> values)
    {
        return string.Join(",", values.Select(ConvertToFilterString).Where(v => v is not null));
    }
}

#endregion

#region Non-Generic FilterBuilder

/// <summary>
/// Fluent builder for constructing <see cref="FilterModel"/> instances with string-based field names.
/// For type-safe field selection, use <see cref="FilterBuilder{T}"/> instead.
/// <example>
/// <code>
/// var filter = FilterBuilder.Create()
///     .Search("laptop")
///     .WhereGreaterThanOrEqual(nameof(Product.Price), "500")
///     .WhereEquals(nameof(Product.CategoryId), "1")
///     .OrderByDescending(nameof(Product.CreatedAt))
///     .Build();
/// </code>
/// </example>
/// </summary>
public class FilterBuilder
{
    private string? _searchTerm;
    private readonly List<FilterItem> _filters = [];
    private readonly List<FilterGroup> _groups = [];
    private readonly List<SortItem> _sorts = [];

    /// <summary>
    /// Protected constructor. Use <see cref="Create"/> factory method.
    /// </summary>
    protected FilterBuilder()
    {
    }

    /// <summary>
    /// Creates a new <see cref="FilterBuilder"/> instance.
    /// </summary>
    public static FilterBuilder Create() => new();

    // ── Search ───────────────────────────────────────────────────────

    /// <summary>
    /// Sets the full-text search term matched against all string properties of the entity.
    /// </summary>
    public FilterBuilder Search(string? searchTerm)
    {
        _searchTerm = searchTerm;
        return this;
    }

    // ── Where (core) ─────────────────────────────────────────────────

    /// <summary>
    /// Adds a filter with the specified field, operator, and optional value.
    /// </summary>
    public FilterBuilder Where(string field, FilterOperator op, string? value = null)
    {
        _filters.Add(new FilterItem(field, op, value));
        return this;
    }

    /// <summary>
    /// Conditionally adds a filter. The filter is only added when <paramref name="condition"/> is true.
    /// </summary>
    public FilterBuilder WhereIf(bool condition, string field, FilterOperator op, string? value = null)
    {
        if (condition)
            _filters.Add(new FilterItem(field, op, value));
        return this;
    }

    // ── Typed shortcut methods ───────────────────────────────────────

    /// <summary>Adds an equality filter (property == value).</summary>
    public FilterBuilder WhereEquals(string field, string value)
        => Where(field, FilterOperator.Equals, value);

    /// <summary>Adds an inequality filter (property != value).</summary>
    public FilterBuilder WhereNotEquals(string field, string value)
        => Where(field, FilterOperator.NotEquals, value);

    /// <summary>Adds a substring match filter. String properties only.</summary>
    public FilterBuilder WhereContains(string field, string value)
        => Where(field, FilterOperator.Contains, value);

    /// <summary>Adds a prefix match filter. String properties only.</summary>
    public FilterBuilder WhereStartsWith(string field, string value)
        => Where(field, FilterOperator.StartsWith, value);

    /// <summary>Adds a suffix match filter. String properties only.</summary>
    public FilterBuilder WhereEndsWith(string field, string value)
        => Where(field, FilterOperator.EndsWith, value);

    /// <summary>Adds a greater-than filter.</summary>
    public FilterBuilder WhereGreaterThan(string field, string value)
        => Where(field, FilterOperator.GreaterThan, value);

    /// <summary>Adds a greater-than-or-equal filter.</summary>
    public FilterBuilder WhereGreaterThanOrEqual(string field, string value)
        => Where(field, FilterOperator.GreaterThanOrEqual, value);

    /// <summary>Adds a less-than filter.</summary>
    public FilterBuilder WhereLessThan(string field, string value)
        => Where(field, FilterOperator.LessThan, value);

    /// <summary>Adds a less-than-or-equal filter.</summary>
    public FilterBuilder WhereLessThanOrEqual(string field, string value)
        => Where(field, FilterOperator.LessThanOrEqual, value);

    /// <summary>Adds a null check filter. No value required.</summary>
    public FilterBuilder WhereIsNull(string field)
        => Where(field, FilterOperator.IsNull);

    /// <summary>Adds a not-null check filter. No value required.</summary>
    public FilterBuilder WhereIsNotNull(string field)
        => Where(field, FilterOperator.IsNotNull);

    /// <summary>Adds an empty string check filter. String properties only.</summary>
    public FilterBuilder WhereIsEmpty(string field)
        => Where(field, FilterOperator.IsEmpty);

    /// <summary>Adds a non-empty string check filter. String properties only.</summary>
    public FilterBuilder WhereIsNotEmpty(string field)
        => Where(field, FilterOperator.IsNotEmpty);

    /// <summary>Adds a set membership filter (IN). Provide comma-separated values.</summary>
    public FilterBuilder WhereIn(string field, string values)
        => Where(field, FilterOperator.In, values);

    /// <summary>Adds a negated set membership filter (NOT IN). Provide comma-separated values.</summary>
    public FilterBuilder WhereNotIn(string field, string values)
        => Where(field, FilterOperator.NotIn, values);

    /// <summary>
    /// Adds two filters for a between range (inclusive): <c>field &gt;= min AND field &lt;= max</c>.
    /// </summary>
    public FilterBuilder WhereBetween(string field, string min, string max)
    {
        _filters.Add(new FilterItem(field, FilterOperator.GreaterThanOrEqual, min));
        _filters.Add(new FilterItem(field, FilterOperator.LessThanOrEqual, max));
        return this;
    }

    // ── OR / AND Groups ──────────────────────────────────────────────

    /// <summary>
    /// Adds an OR group — filters inside are combined with OR logic.
    /// The resulting group is combined with AND against other filters/groups.
    /// <example>
    /// <code>
    /// .OrGroup(g => g
    ///     .Where("Price", FilterOperator.GreaterThan, "1000")
    ///     .Where("IsFeatured", FilterOperator.Equals, "true"))
    /// // SQL: ... AND (Price > 1000 OR IsFeatured = 1)
    /// </code>
    /// </example>
    /// </summary>
    public FilterBuilder OrGroup(Action<FilterGroupBuilder> configure)
    {
        var builder = new FilterGroupBuilder();
        configure(builder);
        _groups.Add(new FilterGroup { Logic = FilterLogic.Or, Filters = builder.Build() });
        return this;
    }

    /// <summary>
    /// Adds an AND group — filters inside are combined with AND logic.
    /// Useful for grouping related conditions when mixing with OR groups.
    /// </summary>
    public FilterBuilder AndGroup(Action<FilterGroupBuilder> configure)
    {
        var builder = new FilterGroupBuilder();
        configure(builder);
        _groups.Add(new FilterGroup { Logic = FilterLogic.And, Filters = builder.Build() });
        return this;
    }

    // ── Sorting ──────────────────────────────────────────────────────

    /// <summary>
    /// Adds an ascending sort criterion. Multiple calls chain as ThenBy.
    /// </summary>
    public FilterBuilder OrderBy(string field)
    {
        _sorts.Add(new SortItem(field, SortDirection.Ascending));
        return this;
    }

    /// <summary>
    /// Adds a descending sort criterion. Multiple calls chain as ThenByDescending.
    /// </summary>
    public FilterBuilder OrderByDescending(string field)
    {
        _sorts.Add(new SortItem(field, SortDirection.Descending));
        return this;
    }

    // ── Build ────────────────────────────────────────────────────────

    /// <summary>
    /// Builds and returns the configured <see cref="FilterModel"/>.
    /// </summary>
    public FilterModel Build()
    {
        return new FilterModel
        {
            SearchTerm = _searchTerm,
            Filters = [.._filters],
            FilterGroups = [.._groups],
            Sorts = [.._sorts]
        };
    }
}

#endregion

#region Non-Generic FilterGroupBuilder

/// <summary>
/// Builder for constructing a list of <see cref="FilterItem"/> within a <see cref="FilterGroup"/>.
/// Used inside <see cref="FilterBuilder.OrGroup"/> and <see cref="FilterBuilder.AndGroup"/>.
/// </summary>
public class FilterGroupBuilder
{
    private readonly List<FilterItem> _filters = [];

    /// <summary>
    /// Adds a filter with the specified field, operator, and optional value.
    /// </summary>
    public FilterGroupBuilder Where(string field, FilterOperator op, string? value = null)
    {
        _filters.Add(new FilterItem(field, op, value));
        return this;
    }

    /// <summary>Adds an equality filter.</summary>
    public FilterGroupBuilder WhereEquals(string field, string value)
        => Where(field, FilterOperator.Equals, value);

    /// <summary>Adds an inequality filter.</summary>
    public FilterGroupBuilder WhereNotEquals(string field, string value)
        => Where(field, FilterOperator.NotEquals, value);

    /// <summary>Adds a substring match filter.</summary>
    public FilterGroupBuilder WhereContains(string field, string value)
        => Where(field, FilterOperator.Contains, value);

    /// <summary>Adds a greater-than filter.</summary>
    public FilterGroupBuilder WhereGreaterThan(string field, string value)
        => Where(field, FilterOperator.GreaterThan, value);

    /// <summary>Adds a greater-than-or-equal filter.</summary>
    public FilterGroupBuilder WhereGreaterThanOrEqual(string field, string value)
        => Where(field, FilterOperator.GreaterThanOrEqual, value);

    /// <summary>Adds a less-than filter.</summary>
    public FilterGroupBuilder WhereLessThan(string field, string value)
        => Where(field, FilterOperator.LessThan, value);

    /// <summary>Adds a less-than-or-equal filter.</summary>
    public FilterGroupBuilder WhereLessThanOrEqual(string field, string value)
        => Where(field, FilterOperator.LessThanOrEqual, value);

    /// <summary>Adds a null check filter.</summary>
    public FilterGroupBuilder WhereIsNull(string field)
        => Where(field, FilterOperator.IsNull);

    /// <summary>Adds a not-null check filter.</summary>
    public FilterGroupBuilder WhereIsNotNull(string field)
        => Where(field, FilterOperator.IsNotNull);

    /// <summary>Adds a set membership filter (IN).</summary>
    public FilterGroupBuilder WhereIn(string field, string values)
        => Where(field, FilterOperator.In, values);

    /// <summary>Adds a negated set membership filter (NOT IN).</summary>
    public FilterGroupBuilder WhereNotIn(string field, string values)
        => Where(field, FilterOperator.NotIn, values);

    /// <summary>
    /// Returns the collected filter items. Called internally by the parent builder.
    /// </summary>
    internal List<FilterItem> Build() => [.._filters];
}

#endregion

#region Generic FilterBuilder<T>

/// <summary>
/// Strongly-typed fluent builder for constructing <see cref="FilterModel"/> instances.
/// Uses lambda expressions for compile-time validated field selection with full IntelliSense support.
/// <para>
/// This is the recommended way to build filters in application code.
/// String-based <see cref="FilterBuilder"/> remains available for JSON/API deserialization scenarios.
/// </para>
/// <example>
/// <code>
/// var filter = FilterBuilder&lt;Product&gt;.Create()
///     .Search("laptop")
///     .Where(p => p.Price, FilterOperator.GreaterThanOrEqual, 500m)
///     .WhereEquals(p => p.CategoryId, 1)
///     .WhereIsNull(p => p.DeletedAt)
///     .WhereIn(p => p.Status, 1, 2, 3)
///     .WhereBetween(p => p.Price, 100m, 999m)
///     .ApplyScope(new ActiveProductScope())
///     .OrGroup(g => g
///         .Where(p => p.IsFeatured, FilterOperator.Equals, true)
///         .WhereGreaterThan(p => p.Rating, 4.5))
///     .OrderByDescending(p => p.CreatedAt)
///     .OrderBy(p => p.Name)
///     .Build();
/// </code>
/// </example>
/// </summary>
/// <typeparam name="T">The entity type whose properties are used for filter field selection.</typeparam>
public class FilterBuilder<T>
{
    private string? _searchTerm;
    private readonly List<FilterItem> _filters = [];
    private readonly List<FilterGroup> _groups = [];
    private readonly List<SortItem> _sorts = [];

    private FilterBuilder()
    {
    }

    /// <summary>
    /// Creates a new strongly-typed <see cref="FilterBuilder{T}"/> instance.
    /// </summary>
    public static FilterBuilder<T> Create() => new();

    // ── Search ───────────────────────────────────────────────────────

    /// <summary>
    /// Sets the full-text search term matched against all string properties of <typeparamref name="T"/>.
    /// </summary>
    public FilterBuilder<T> Search(string? searchTerm)
    {
        _searchTerm = searchTerm;
        return this;
    }

    // ── Where (core) ─────────────────────────────────────────────────

    /// <summary>
    /// Adds a filter using a compile-time validated field expression.
    /// </summary>
    /// <param name="field">Lambda selecting the property (e.g., <c>p => p.Price</c> or <c>p => p.Category.Name</c>).</param>
    /// <param name="op">The filter operator.</param>
    /// <param name="value">The filter value. Automatically converted to string using invariant culture.</param>
    public FilterBuilder<T> Where(Expression<Func<T, object?>> field, FilterOperator op, object? value = null)
    {
        _filters.Add(new FilterItem(
            FilterExpressionHelper.ExtractPropertyPath(field),
            op,
            FilterExpressionHelper.ConvertToFilterString(value)));
        return this;
    }

    /// <summary>
    /// Conditionally adds a filter. The filter is only added when <paramref name="condition"/> is true.
    /// </summary>
    public FilterBuilder<T> WhereIf(bool condition, Expression<Func<T, object?>> field, FilterOperator op, object? value = null)
    {
        if (condition)
            Where(field, op, value);
        return this;
    }

    // ── Typed shortcut methods ───────────────────────────────────────

    /// <summary>Adds an equality filter (property == value).</summary>
    public FilterBuilder<T> WhereEquals(Expression<Func<T, object?>> field, object value)
        => Where(field, FilterOperator.Equals, value);

    /// <summary>Adds an inequality filter (property != value).</summary>
    public FilterBuilder<T> WhereNotEquals(Expression<Func<T, object?>> field, object value)
        => Where(field, FilterOperator.NotEquals, value);

    /// <summary>Adds a substring match filter. String properties only.</summary>
    public FilterBuilder<T> WhereContains(Expression<Func<T, object?>> field, string value)
        => Where(field, FilterOperator.Contains, value);

    /// <summary>Adds a prefix match filter. String properties only.</summary>
    public FilterBuilder<T> WhereStartsWith(Expression<Func<T, object?>> field, string value)
        => Where(field, FilterOperator.StartsWith, value);

    /// <summary>Adds a suffix match filter. String properties only.</summary>
    public FilterBuilder<T> WhereEndsWith(Expression<Func<T, object?>> field, string value)
        => Where(field, FilterOperator.EndsWith, value);

    /// <summary>Adds a greater-than filter.</summary>
    public FilterBuilder<T> WhereGreaterThan(Expression<Func<T, object?>> field, object value)
        => Where(field, FilterOperator.GreaterThan, value);

    /// <summary>Adds a greater-than-or-equal filter.</summary>
    public FilterBuilder<T> WhereGreaterThanOrEqual(Expression<Func<T, object?>> field, object value)
        => Where(field, FilterOperator.GreaterThanOrEqual, value);

    /// <summary>Adds a less-than filter.</summary>
    public FilterBuilder<T> WhereLessThan(Expression<Func<T, object?>> field, object value)
        => Where(field, FilterOperator.LessThan, value);

    /// <summary>Adds a less-than-or-equal filter.</summary>
    public FilterBuilder<T> WhereLessThanOrEqual(Expression<Func<T, object?>> field, object value)
        => Where(field, FilterOperator.LessThanOrEqual, value);

    /// <summary>Adds a null check filter. No value required.</summary>
    public FilterBuilder<T> WhereIsNull(Expression<Func<T, object?>> field)
        => Where(field, FilterOperator.IsNull);

    /// <summary>Adds a not-null check filter. No value required.</summary>
    public FilterBuilder<T> WhereIsNotNull(Expression<Func<T, object?>> field)
        => Where(field, FilterOperator.IsNotNull);

    /// <summary>Adds an empty string check filter. String properties only.</summary>
    public FilterBuilder<T> WhereIsEmpty(Expression<Func<T, object?>> field)
        => Where(field, FilterOperator.IsEmpty);

    /// <summary>Adds a non-empty string check filter. String properties only.</summary>
    public FilterBuilder<T> WhereIsNotEmpty(Expression<Func<T, object?>> field)
        => Where(field, FilterOperator.IsNotEmpty);

    /// <summary>
    /// Adds a set membership filter (IN). Values are converted to a comma-separated string.
    /// </summary>
    /// <param name="field">The property to filter.</param>
    /// <param name="values">The values to include in the set.</param>
    public FilterBuilder<T> WhereIn(Expression<Func<T, object?>> field, params object[] values)
    {
        var csv = FilterExpressionHelper.ConvertToCommaSeparated(values);
        return Where(field, FilterOperator.In, csv);
    }

    /// <summary>
    /// Adds a negated set membership filter (NOT IN). Values are converted to a comma-separated string.
    /// </summary>
    /// <param name="field">The property to filter.</param>
    /// <param name="values">The values to exclude from results.</param>
    public FilterBuilder<T> WhereNotIn(Expression<Func<T, object?>> field, params object[] values)
    {
        var csv = FilterExpressionHelper.ConvertToCommaSeparated(values);
        return Where(field, FilterOperator.NotIn, csv);
    }

    // ── Convenience: Between / DateRange ─────────────────────────────

    /// <summary>
    /// Adds two filters for an inclusive range: <c>field &gt;= min AND field &lt;= max</c>.
    /// Works with any comparable type (numeric, DateTime, DateOnly, etc.).
    /// </summary>
    public FilterBuilder<T> WhereBetween(Expression<Func<T, object?>> field, object min, object max)
    {
        var fieldName = FilterExpressionHelper.ExtractPropertyPath(field);
        _filters.Add(new FilterItem(fieldName, FilterOperator.GreaterThanOrEqual, FilterExpressionHelper.ConvertToFilterString(min)));
        _filters.Add(new FilterItem(fieldName, FilterOperator.LessThanOrEqual, FilterExpressionHelper.ConvertToFilterString(max)));
        return this;
    }

    /// <summary>
    /// Adds two filters for a date/time range (inclusive): <c>field &gt;= start AND field &lt;= end</c>.
    /// Semantically identical to <see cref="WhereBetween"/> but named for clarity with date fields.
    /// </summary>
    public FilterBuilder<T> WhereDateRange(Expression<Func<T, object?>> field, DateTime start, DateTime end)
        => WhereBetween(field, start, end);

    // ── OR / AND Groups ──────────────────────────────────────────────

    /// <summary>
    /// Adds an OR group — filters inside are combined with OR logic.
    /// <example>
    /// <code>
    /// .OrGroup(g => g
    ///     .Where(p => p.Price, FilterOperator.GreaterThan, 1000m)
    ///     .WhereEquals(p => p.IsFeatured, true))
    /// // SQL: ... AND (Price &gt; 1000 OR IsFeatured = 1)
    /// </code>
    /// </example>
    /// </summary>
    public FilterBuilder<T> OrGroup(Action<FilterGroupBuilder<T>> configure)
    {
        var builder = new FilterGroupBuilder<T>();
        configure(builder);
        _groups.Add(new FilterGroup { Logic = FilterLogic.Or, Filters = builder.Build() });
        return this;
    }

    /// <summary>
    /// Adds an AND group — filters inside are combined with AND logic.
    /// </summary>
    public FilterBuilder<T> AndGroup(Action<FilterGroupBuilder<T>> configure)
    {
        var builder = new FilterGroupBuilder<T>();
        configure(builder);
        _groups.Add(new FilterGroup { Logic = FilterLogic.And, Filters = builder.Build() });
        return this;
    }

    // ── Sorting ──────────────────────────────────────────────────────

    /// <summary>
    /// Adds an ascending sort criterion. Multiple calls chain as ThenBy.
    /// </summary>
    public FilterBuilder<T> OrderBy(Expression<Func<T, object?>> field)
    {
        _sorts.Add(new SortItem(FilterExpressionHelper.ExtractPropertyPath(field), SortDirection.Ascending));
        return this;
    }

    /// <summary>
    /// Adds a descending sort criterion. Multiple calls chain as ThenByDescending.
    /// </summary>
    public FilterBuilder<T> OrderByDescending(Expression<Func<T, object?>> field)
    {
        _sorts.Add(new SortItem(FilterExpressionHelper.ExtractPropertyPath(field), SortDirection.Descending));
        return this;
    }

    // ── Scopes ───────────────────────────────────────────────────────

    /// <summary>
    /// Applies a reusable filter scope to this builder.
    /// Scopes encapsulate commonly used filter combinations for DRY composition.
    /// </summary>
    /// <param name="scope">The filter scope to apply.</param>
    public FilterBuilder<T> ApplyScope(IFilterScope<T> scope)
    {
        scope.Apply(this);
        return this;
    }

    // ── Build ────────────────────────────────────────────────────────

    /// <summary>
    /// Builds and returns the configured <see cref="FilterModel"/>.
    /// </summary>
    public FilterModel Build()
    {
        return new FilterModel
        {
            SearchTerm = _searchTerm,
            Filters = [.._filters],
            FilterGroups = [.._groups],
            Sorts = [.._sorts]
        };
    }
}

#endregion

#region Generic FilterGroupBuilder<T>

/// <summary>
/// Strongly-typed builder for constructing filter items within a <see cref="FilterGroup"/>.
/// Used inside <see cref="FilterBuilder{T}.OrGroup"/> and <see cref="FilterBuilder{T}.AndGroup"/>.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public class FilterGroupBuilder<T>
{
    private readonly List<FilterItem> _filters = [];

    /// <summary>
    /// Adds a filter using a compile-time validated field expression.
    /// </summary>
    public FilterGroupBuilder<T> Where(Expression<Func<T, object?>> field, FilterOperator op, object? value = null)
    {
        _filters.Add(new FilterItem(
            FilterExpressionHelper.ExtractPropertyPath(field),
            op,
            FilterExpressionHelper.ConvertToFilterString(value)));
        return this;
    }

    /// <summary>Adds an equality filter.</summary>
    public FilterGroupBuilder<T> WhereEquals(Expression<Func<T, object?>> field, object value)
        => Where(field, FilterOperator.Equals, value);

    /// <summary>Adds an inequality filter.</summary>
    public FilterGroupBuilder<T> WhereNotEquals(Expression<Func<T, object?>> field, object value)
        => Where(field, FilterOperator.NotEquals, value);

    /// <summary>Adds a substring match filter.</summary>
    public FilterGroupBuilder<T> WhereContains(Expression<Func<T, object?>> field, string value)
        => Where(field, FilterOperator.Contains, value);

    /// <summary>Adds a prefix match filter.</summary>
    public FilterGroupBuilder<T> WhereStartsWith(Expression<Func<T, object?>> field, string value)
        => Where(field, FilterOperator.StartsWith, value);

    /// <summary>Adds a suffix match filter.</summary>
    public FilterGroupBuilder<T> WhereEndsWith(Expression<Func<T, object?>> field, string value)
        => Where(field, FilterOperator.EndsWith, value);

    /// <summary>Adds a greater-than filter.</summary>
    public FilterGroupBuilder<T> WhereGreaterThan(Expression<Func<T, object?>> field, object value)
        => Where(field, FilterOperator.GreaterThan, value);

    /// <summary>Adds a greater-than-or-equal filter.</summary>
    public FilterGroupBuilder<T> WhereGreaterThanOrEqual(Expression<Func<T, object?>> field, object value)
        => Where(field, FilterOperator.GreaterThanOrEqual, value);

    /// <summary>Adds a less-than filter.</summary>
    public FilterGroupBuilder<T> WhereLessThan(Expression<Func<T, object?>> field, object value)
        => Where(field, FilterOperator.LessThan, value);

    /// <summary>Adds a less-than-or-equal filter.</summary>
    public FilterGroupBuilder<T> WhereLessThanOrEqual(Expression<Func<T, object?>> field, object value)
        => Where(field, FilterOperator.LessThanOrEqual, value);

    /// <summary>Adds a null check filter.</summary>
    public FilterGroupBuilder<T> WhereIsNull(Expression<Func<T, object?>> field)
        => Where(field, FilterOperator.IsNull);

    /// <summary>Adds a not-null check filter.</summary>
    public FilterGroupBuilder<T> WhereIsNotNull(Expression<Func<T, object?>> field)
        => Where(field, FilterOperator.IsNotNull);

    /// <summary>Adds an empty string check filter.</summary>
    public FilterGroupBuilder<T> WhereIsEmpty(Expression<Func<T, object?>> field)
        => Where(field, FilterOperator.IsEmpty);

    /// <summary>Adds a non-empty string check filter.</summary>
    public FilterGroupBuilder<T> WhereIsNotEmpty(Expression<Func<T, object?>> field)
        => Where(field, FilterOperator.IsNotEmpty);

    /// <summary>Adds a set membership filter (IN).</summary>
    public FilterGroupBuilder<T> WhereIn(Expression<Func<T, object?>> field, params object[] values)
    {
        var csv = FilterExpressionHelper.ConvertToCommaSeparated(values);
        return Where(field, FilterOperator.In, csv);
    }

    /// <summary>Adds a negated set membership filter (NOT IN).</summary>
    public FilterGroupBuilder<T> WhereNotIn(Expression<Func<T, object?>> field, params object[] values)
    {
        var csv = FilterExpressionHelper.ConvertToCommaSeparated(values);
        return Where(field, FilterOperator.NotIn, csv);
    }

    /// <summary>
    /// Returns the collected filter items. Called internally by the parent builder.
    /// </summary>
    internal List<FilterItem> Build() => [.._filters];
}

#endregion
