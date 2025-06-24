namespace FS.EntityFramework.Library.Paging;

/// <summary>
/// Represents a paginated collection of items with metadata
/// </summary>
/// <typeparam name="T">The type of items in the collection</typeparam>
public interface IPaginate<T>
{
    /// <summary>
    /// Gets the starting index (typically 0 or 1)
    /// </summary>
    int From { get; }
    
    /// <summary>
    /// Gets the current page index
    /// </summary>
    int Index { get; }
    
    /// <summary>
    /// Gets the page size (number of items per page)
    /// </summary>
    int Size { get; }
    
    /// <summary>
    /// Gets the total number of items across all pages
    /// </summary>
    int Count { get; }
    
    /// <summary>
    /// Gets the total number of pages
    /// </summary>
    int Pages { get; }
    
    /// <summary>
    /// Gets the items in the current page
    /// </summary>
    IList<T> Items { get; }
    
    /// <summary>
    /// Gets a value indicating whether there is a previous page
    /// </summary>
    bool HasPrevious { get; }
    
    /// <summary>
    /// Gets a value indicating whether there is a next page
    /// </summary>
    bool HasNext { get; }
}