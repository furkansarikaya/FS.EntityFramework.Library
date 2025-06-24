namespace FS.EntityFramework.Library.Models;

/// <summary>
/// Dinamik filtreleme için model
/// </summary>
public class FilterModel
{
    public string? SearchTerm { get; set; }
    public List<FilterItem> Filters { get; set; } = [];
}

public class FilterItem
{
    public string Field { get; set; } = string.Empty;
    public string Operator { get; set; } = "equals"; // equals, contains, greaterThan, lessThan, etc.
    public string Value { get; set; } = string.Empty;
}