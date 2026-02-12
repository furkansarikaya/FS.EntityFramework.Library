namespace FS.EntityFramework.Library.Models;

/// <summary>
/// Model for dynamic filtering, sorting, and grouping of entities.
/// Contains an optional full-text search term, a list of field-level filter criteria,
/// logical filter groups (for OR/AND composition), and sort criteria.
/// <para>
/// Use <see cref="FilterBuilder"/> or <see cref="FilterBuilder{T}"/> fluent API
/// for type-safe construction.
/// </para>
/// </summary>
public class FilterModel
{
    /// <summary>
    /// Gets or sets the full-text search term that is matched against all string properties of the entity.
    /// When set, the search is combined with field-level filters using AND logic.
    /// </summary>
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Gets or sets the list of field-level filter criteria.
    /// Multiple filters are combined using AND logic.
    /// </summary>
    public List<FilterItem> Filters { get; set; } = [];

    /// <summary>
    /// Gets or sets the list of filter groups for complex logical composition.
    /// Each group's internal filters are combined using the group's <see cref="FilterGroup.Logic"/> (AND or OR).
    /// Multiple groups are combined with AND between them.
    /// <para>
    /// Example: <c>WHERE (base filters) AND (group1: A OR B) AND (group2: C AND D)</c>
    /// </para>
    /// </summary>
    public List<FilterGroup> FilterGroups { get; set; } = [];

    /// <summary>
    /// Gets or sets the list of sort criteria.
    /// Sort items are applied in order: the first becomes <c>OrderBy</c>,
    /// subsequent items become <c>ThenBy</c>.
    /// <para>
    /// When used with repository methods that accept an <c>orderBy</c> parameter,
    /// the explicit <c>orderBy</c> parameter takes precedence over this list.
    /// </para>
    /// </summary>
    public List<SortItem> Sorts { get; set; } = [];
}

/// <summary>
/// Represents a single field-level filter criterion for dynamic filtering.
/// Supports both string-based operator syntax (for JSON/API deserialization)
/// and type-safe <see cref="FilterOperator"/> enum via the constructor overload.
/// </summary>
public class FilterItem
{
    /// <summary>
    /// Initializes a new instance of <see cref="FilterItem"/> with default values.
    /// Required for JSON deserialization and API model binding.
    /// </summary>
    public FilterItem()
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="FilterItem"/> with a type-safe <see cref="FilterOperator"/>.
    /// Provides compile-time validation and IntelliSense support for filter operators.
    /// </summary>
    /// <param name="field">The entity property name to filter on (case-insensitive, supports dot notation for navigation properties).</param>
    /// <param name="op">The type-safe filter operator enum value.</param>
    /// <param name="value">
    /// The filter value as a string. Optional for value-less operators
    /// (<see cref="FilterOperator.IsNull"/>, <see cref="FilterOperator.IsNotNull"/>,
    /// <see cref="FilterOperator.IsEmpty"/>, <see cref="FilterOperator.IsNotEmpty"/>).
    /// For <see cref="FilterOperator.In"/> and <see cref="FilterOperator.NotIn"/>, provide comma-separated values.
    /// </param>
    public FilterItem(string field, FilterOperator op, string? value = null)
    {
        Field = field;
        Operator = op.ToFilterString();
        Value = value;
    }

    /// <summary>
    /// Gets or sets the entity property name to filter on.
    /// Supports dot notation for navigation properties (e.g., "Category.Name").
    /// Property name matching is case-insensitive.
    /// </summary>
    public string Field { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the filter operator as a string.
    /// Accepts canonical names ("equals", "greaterthanorequal") and short aliases ("eq", "gte").
    /// See <see cref="FilterOperator"/> for the full list of supported operators.
    /// </summary>
    public string Operator { get; set; } = "equals";

    /// <summary>
    /// Gets or sets the filter value.
    /// Null is allowed for value-less operators (IsNull, IsNotNull, IsEmpty, IsNotEmpty).
    /// For In/NotIn operators, provide comma-separated values (e.g., "1,2,3").
    /// Values are automatically converted to the target property type using invariant culture.
    /// </summary>
    public string? Value { get; set; } = string.Empty;
}
