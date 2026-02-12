namespace FS.EntityFramework.Library.Models;

/// <summary>
/// Fluent builder for constructing <see cref="FilterModel"/> instances with type-safe operators.
/// Provides a readable, discoverable API for building dynamic filter queries.
/// <example>
/// <code>
/// var filter = FilterBuilder.Create()
///     .Search("laptop")
///     .WhereGreaterThanOrEqual(nameof(Product.Price), "500")
///     .WhereEquals(nameof(Product.CategoryId), "1")
///     .WhereIsNull(nameof(Product.DeletedAt))
///     .Build();
/// </code>
/// </example>
/// </summary>
public class FilterBuilder
{
    private string? _searchTerm;
    private readonly List<FilterItem> _filters = [];

    private FilterBuilder()
    {
    }

    /// <summary>
    /// Creates a new <see cref="FilterBuilder"/> instance.
    /// </summary>
    /// <returns>A new builder instance ready for method chaining.</returns>
    public static FilterBuilder Create() => new();

    /// <summary>
    /// Sets the full-text search term that is matched against all string properties of the entity.
    /// Null or empty values are ignored.
    /// </summary>
    /// <param name="searchTerm">The text to search for across all string properties.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public FilterBuilder Search(string? searchTerm)
    {
        _searchTerm = searchTerm;
        return this;
    }

    /// <summary>
    /// Adds a filter with the specified field, operator, and optional value.
    /// </summary>
    /// <param name="field">The entity property name to filter on.</param>
    /// <param name="op">The type-safe filter operator.</param>
    /// <param name="value">The filter value (optional for value-less operators).</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public FilterBuilder Where(string field, FilterOperator op, string? value = null)
    {
        _filters.Add(new FilterItem(field, op, value));
        return this;
    }

    /// <summary>
    /// Conditionally adds a filter. The filter is only added when <paramref name="condition"/> is true.
    /// Useful for building dynamic filters from optional request parameters.
    /// </summary>
    /// <param name="condition">When true, the filter is added; when false, the call is a no-op.</param>
    /// <param name="field">The entity property name to filter on.</param>
    /// <param name="op">The type-safe filter operator.</param>
    /// <param name="value">The filter value (optional for value-less operators).</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public FilterBuilder WhereIf(bool condition, string field, FilterOperator op, string? value = null)
    {
        if (condition)
            _filters.Add(new FilterItem(field, op, value));
        return this;
    }

    /// <summary>
    /// Adds an equality filter (property == value).
    /// </summary>
    public FilterBuilder WhereEquals(string field, string value)
        => Where(field, FilterOperator.Equals, value);

    /// <summary>
    /// Adds an inequality filter (property != value).
    /// </summary>
    public FilterBuilder WhereNotEquals(string field, string value)
        => Where(field, FilterOperator.NotEquals, value);

    /// <summary>
    /// Adds a substring match filter (property.Contains(value)). String properties only.
    /// </summary>
    public FilterBuilder WhereContains(string field, string value)
        => Where(field, FilterOperator.Contains, value);

    /// <summary>
    /// Adds a prefix match filter (property.StartsWith(value)). String properties only.
    /// </summary>
    public FilterBuilder WhereStartsWith(string field, string value)
        => Where(field, FilterOperator.StartsWith, value);

    /// <summary>
    /// Adds a suffix match filter (property.EndsWith(value)). String properties only.
    /// </summary>
    public FilterBuilder WhereEndsWith(string field, string value)
        => Where(field, FilterOperator.EndsWith, value);

    /// <summary>
    /// Adds a greater-than filter (property &gt; value).
    /// </summary>
    public FilterBuilder WhereGreaterThan(string field, string value)
        => Where(field, FilterOperator.GreaterThan, value);

    /// <summary>
    /// Adds a greater-than-or-equal filter (property &gt;= value).
    /// </summary>
    public FilterBuilder WhereGreaterThanOrEqual(string field, string value)
        => Where(field, FilterOperator.GreaterThanOrEqual, value);

    /// <summary>
    /// Adds a less-than filter (property &lt; value).
    /// </summary>
    public FilterBuilder WhereLessThan(string field, string value)
        => Where(field, FilterOperator.LessThan, value);

    /// <summary>
    /// Adds a less-than-or-equal filter (property &lt;= value).
    /// </summary>
    public FilterBuilder WhereLessThanOrEqual(string field, string value)
        => Where(field, FilterOperator.LessThanOrEqual, value);

    /// <summary>
    /// Adds a null check filter (property == null). No value parameter required.
    /// Returns false for non-nullable value types.
    /// </summary>
    public FilterBuilder WhereIsNull(string field)
        => Where(field, FilterOperator.IsNull);

    /// <summary>
    /// Adds a not-null check filter (property != null). No value parameter required.
    /// Returns true for non-nullable value types.
    /// </summary>
    public FilterBuilder WhereIsNotNull(string field)
        => Where(field, FilterOperator.IsNotNull);

    /// <summary>
    /// Adds an empty string check filter (property == null || property == ""). String properties only.
    /// No value parameter required. Returns false for non-string types.
    /// </summary>
    public FilterBuilder WhereIsEmpty(string field)
        => Where(field, FilterOperator.IsEmpty);

    /// <summary>
    /// Adds a non-empty string check filter (property != null &amp;&amp; property != ""). String properties only.
    /// No value parameter required. Returns false for non-string types.
    /// </summary>
    public FilterBuilder WhereIsNotEmpty(string field)
        => Where(field, FilterOperator.IsNotEmpty);

    /// <summary>
    /// Adds a set membership filter (WHERE property IN (...)).
    /// Provide values as a comma-separated string (e.g., "1,2,3").
    /// </summary>
    public FilterBuilder WhereIn(string field, string values)
        => Where(field, FilterOperator.In, values);

    /// <summary>
    /// Adds a negated set membership filter (WHERE property NOT IN (...)).
    /// Provide values as a comma-separated string (e.g., "1,2,3").
    /// </summary>
    public FilterBuilder WhereNotIn(string field, string values)
        => Where(field, FilterOperator.NotIn, values);

    /// <summary>
    /// Builds and returns the configured <see cref="FilterModel"/>.
    /// </summary>
    /// <returns>A new <see cref="FilterModel"/> containing the configured search term and filters.</returns>
    public FilterModel Build()
    {
        return new FilterModel
        {
            SearchTerm = _searchTerm,
            Filters = [.._filters]
        };
    }
}
