namespace FS.EntityFramework.Library.Models;

/// <summary>
/// Specifies the direction of a sort operation.
/// </summary>
public enum SortDirection
{
    /// <summary>
    /// Sort in ascending order (A → Z, 0 → 9, oldest → newest).
    /// </summary>
    Ascending,

    /// <summary>
    /// Sort in descending order (Z → A, 9 → 0, newest → oldest).
    /// </summary>
    Descending
}

/// <summary>
/// Represents a single sort criterion for dynamic query ordering.
/// Multiple <see cref="SortItem"/> instances are applied in sequence:
/// the first becomes <c>OrderBy</c>, subsequent ones become <c>ThenBy</c>.
/// </summary>
public class SortItem
{
    /// <summary>
    /// Initializes a new instance of <see cref="SortItem"/> with default values.
    /// Required for JSON deserialization and API model binding.
    /// </summary>
    public SortItem()
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="SortItem"/> with the specified field and direction.
    /// </summary>
    /// <param name="field">The entity property name to sort by (case-insensitive).</param>
    /// <param name="direction">The sort direction (ascending or descending).</param>
    public SortItem(string field, SortDirection direction = SortDirection.Ascending)
    {
        Field = field;
        Direction = direction;
    }

    /// <summary>
    /// Gets or sets the entity property name to sort by.
    /// Property name matching is case-insensitive.
    /// </summary>
    public string Field { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sort direction.
    /// Defaults to <see cref="SortDirection.Ascending"/>.
    /// </summary>
    public SortDirection Direction { get; set; } = SortDirection.Ascending;
}
