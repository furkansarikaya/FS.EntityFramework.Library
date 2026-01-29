namespace FS.EntityFramework.Library.Paging;

/// <summary>
/// Represents a cursor-based paginated result set.
/// Cursor pagination is more efficient than offset pagination for large datasets
/// as it doesn't require counting all previous records.
/// </summary>
/// <typeparam name="T">The type of items in the collection.</typeparam>
/// <typeparam name="TCursor">The type of the cursor value (usually the ID type).</typeparam>
public interface ICursorPaginate<T, TCursor>
{
    /// <summary>
    /// Gets the items in the current page.
    /// </summary>
    IList<T> Items { get; }

    /// <summary>
    /// Gets the requested page size.
    /// </summary>
    int Size { get; }

    /// <summary>
    /// Gets the actual count of items returned (may be less than Size on last page).
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Gets the cursor value of the first item in the current page.
    /// Use this for "previous page" navigation.
    /// </summary>
    TCursor? FirstCursor { get; }

    /// <summary>
    /// Gets the cursor value of the last item in the current page.
    /// Use this for "next page" navigation.
    /// </summary>
    TCursor? LastCursor { get; }

    /// <summary>
    /// Gets a value indicating whether there are more items after the current page.
    /// </summary>
    bool HasNext { get; }

    /// <summary>
    /// Gets a value indicating whether there are items before the current page.
    /// </summary>
    bool HasPrevious { get; }
}
