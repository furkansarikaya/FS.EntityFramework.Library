namespace FS.EntityFramework.Library.Models;

/// <summary>
/// Specifies the logical operator used to combine filters within a <see cref="FilterGroup"/>.
/// </summary>
public enum FilterLogic
{
    /// <summary>
    /// Combines filters with logical AND — all conditions must be true.
    /// </summary>
    And,

    /// <summary>
    /// Combines filters with logical OR — at least one condition must be true.
    /// </summary>
    Or
}

/// <summary>
/// Represents a group of filter criteria combined with a logical operator (AND/OR).
/// Groups enable complex query expressions such as:
/// <c>WHERE IsActive = 1 AND (Price &gt; 1000 OR IsFeatured = 1)</c>
/// <para>
/// Each group's filters are combined using its <see cref="Logic"/> property.
/// Multiple groups are combined with AND between them in <see cref="FilterModel.FilterGroups"/>.
/// </para>
/// <example>
/// <code>
/// var group = new FilterGroup
/// {
///     Logic = FilterLogic.Or,
///     Filters =
///     [
///         new FilterItem(nameof(Product.Price), FilterOperator.GreaterThan, "1000"),
///         new FilterItem(nameof(Product.IsFeatured), FilterOperator.Equals, "true")
///     ]
/// };
/// </code>
/// </example>
/// </summary>
public class FilterGroup
{
    /// <summary>
    /// Gets or sets the logical operator used to combine filters within this group.
    /// Defaults to <see cref="FilterLogic.And"/>.
    /// </summary>
    public FilterLogic Logic { get; set; } = FilterLogic.And;

    /// <summary>
    /// Gets or sets the list of filter criteria in this group.
    /// </summary>
    public List<FilterItem> Filters { get; set; } = [];
}
