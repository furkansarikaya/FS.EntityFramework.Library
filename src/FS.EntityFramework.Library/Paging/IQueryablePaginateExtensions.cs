using Microsoft.EntityFrameworkCore;

namespace FS.EntityFramework.Library.Paging;

/// <summary>
/// Extension methods for IQueryable to provide pagination functionality
/// </summary>
public static class IQueryablePaginateExtensions
{
    /// <summary>
    /// Converts an IQueryable to a paginated result asynchronously
    /// </summary>
    /// <typeparam name="T">The type of items in the queryable</typeparam>
    /// <param name="source">The source queryable</param>
    /// <param name="index">The page index</param>
    /// <param name="size">The page size</param>
    /// <param name="from">The starting index (default: 0)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A paginated result</returns>
    public static async Task<IPaginate<T>> ToPaginateAsync<T>(this IQueryable<T> source, int index, int size,
        int from = 0,
        CancellationToken cancellationToken = default)
    {
        if (from > index) throw new ArgumentException($"From: {from} > Index: {index}, must from <= Index");

        var count = await source.CountAsync(cancellationToken).ConfigureAwait(false);
        var items = await source.Skip((index - from) * size).Take(size).ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        Paginate<T> list = new()
        {
            Index = index,
            Size = size,
            From = from,
            Count = count,
            Items = items,
            Pages = (int)Math.Ceiling(count / (double)size)
        };
        return list;
    }

    /// <summary>
    /// Converts an IQueryable to a paginated result synchronously
    /// </summary>
    /// <typeparam name="T">The type of items in the queryable</typeparam>
    /// <param name="source">The source queryable</param>
    /// <param name="index">The page index</param>
    /// <param name="size">The page size</param>
    /// <param name="from">The starting index (default: 0)</param>
    /// <returns>A paginated result</returns>
    public static IPaginate<T> ToPaginate<T>(this IQueryable<T> source, int index, int size,
        int from = 0)
    {
        if (from > index) throw new ArgumentException($"From: {from} > Index: {index}, must from <= Index");

        var count = source.Count();
        var items = source.Skip((index - from) * size).Take(size).ToList();
        Paginate<T> list = new()
        {
            Index = index,
            Size = size,
            From = from,
            Count = count,
            Items = items,
            Pages = (int)Math.Ceiling(count / (double)size)
        };
        return list;
    }
}