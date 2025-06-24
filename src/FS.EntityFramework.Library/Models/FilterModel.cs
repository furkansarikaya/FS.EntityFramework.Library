namespace FS.EntityFramework.Library.Models;

/// <summary>
/// Model for dynamic filtering of entities.
/// </summary>
public class FilterModel
{
    public string? SearchTerm { get; set; }
    public List<FilterItem> Filters { get; set; } = [];
}

/// <summary>
/// Represents a filter item for dynamic filtering
/// </summary>
public class FilterItem
{
    /// <summary>
    /// Gets or sets the field name to filter on
    /// </summary>
    public string Field { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the filter operator (equals, contains, greaterThan, lessThan, etc.)
    /// </summary>
    public string Operator { get; set; } = "equals"; // equals, contains, greaterThan, lessThan, etc.
    
    /// <summary>
    /// Gets or sets the filter value
    /// </summary>
    public string Value { get; set; } = string.Empty;
}