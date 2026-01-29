namespace FS.EntityFramework.Library.Paging;

/// <summary>
/// Default implementation of cursor-based pagination.
/// </summary>
/// <typeparam name="T">The type of items in the collection.</typeparam>
/// <typeparam name="TCursor">The type of the cursor value.</typeparam>
public class CursorPaginate<T, TCursor> : ICursorPaginate<T, TCursor>
{
    /// <inheritdoc />
    public IList<T> Items { get; init; } = new List<T>();

    /// <inheritdoc />
    public int Size { get; init; }

    /// <inheritdoc />
    public int Count => Items.Count;

    /// <inheritdoc />
    public TCursor? FirstCursor { get; init; }

    /// <inheritdoc />
    public TCursor? LastCursor { get; init; }

    /// <inheritdoc />
    public bool HasNext { get; init; }

    /// <inheritdoc />
    public bool HasPrevious { get; init; }
}
