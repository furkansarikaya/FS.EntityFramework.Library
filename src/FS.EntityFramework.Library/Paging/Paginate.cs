namespace FS.EntityFramework.Library.Paging;

/// <summary>
/// Implementation of IPaginate that provides pagination functionality
/// </summary>
/// <typeparam name="T">The type of items in the paginated collection</typeparam>
public class Paginate<T> : IPaginate<T>
{
    /// <summary>
    /// Initializes a new instance of the Paginate class with source data
    /// </summary>
    /// <param name="source">The source enumerable</param>
    /// <param name="index">The page index</param>
    /// <param name="size">The page size</param>
    /// <param name="from">The starting index</param>
    internal Paginate(IEnumerable<T> source, int index, int size, int from)
    {
        var enumerable = source as T[] ?? source.ToArray();

        if (from > index)
            throw new ArgumentException($"indexFrom: {from} > pageIndex: {index}, must indexFrom <= pageIndex");

        if (source is IQueryable<T> querable)
        {
            Index = index;
            Size = size;
            From = from;
            Count = querable.Count();
            Pages = (int)Math.Ceiling(Count / (double)Size);

            Items = querable.Skip((Index - From) * Size).Take(Size).ToList();
        }
        else
        {
            Index = index;
            Size = size;
            From = from;

            Count = enumerable.Count();
            Pages = (int)Math.Ceiling(Count / (double)Size);

            Items = enumerable.Skip((Index - From) * Size).Take(Size).ToList();
        }
    }

    /// <summary>
    /// Initializes a new empty instance of the Paginate class
    /// </summary>
    internal Paginate()
    {
        Items = new T[0];
    }

    public int From { get; set; }
    public int Index { get; set; }
    public int Size { get; set; }
    public int Count { get; set; }
    public int Pages { get; set; }
    public IList<T> Items { get; set; }
    public bool HasPrevious => Index - From > 0;
    public bool HasNext => Index - From + 1 < Pages;
}

internal class Paginate<TSource, TResult> : IPaginate<TResult>
{
    public Paginate(IEnumerable<TSource> source, Func<IEnumerable<TSource>, IEnumerable<TResult>> converter,
                    int index, int size, int from)
    {
        var enumerable = source as TSource[] ?? source.ToArray();

        if (from > index) throw new ArgumentException($"From: {from} > Index: {index}, must From <= Index");

        if (source is IQueryable<TSource> queryable)
        {
            Index = index;
            Size = size;
            From = from;
            Count = queryable.Count();
            Pages = (int)Math.Ceiling(Count / (double)Size);

            var items = queryable.Skip((Index - From) * Size).Take(Size).ToArray();

            Items = new List<TResult>(converter(items));
        }
        else
        {
            Index = index;
            Size = size;
            From = from;
            Count = enumerable.Count();
            Pages = (int)Math.Ceiling(Count / (double)Size);

            var items = enumerable.Skip((Index - From) * Size).Take(Size).ToArray();

            Items = new List<TResult>(converter(items));
        }
    }


    public Paginate(IPaginate<TSource> source, Func<IEnumerable<TSource>, IEnumerable<TResult>> converter)
    {
        Index = source.Index;
        Size = source.Size;
        From = source.From;
        Count = source.Count;
        Pages = source.Pages;

        Items = new List<TResult>(converter(source.Items));
    }

    public int Index { get; }

    public int Size { get; }

    public int Count { get; }

    public int Pages { get; }

    public int From { get; }

    public IList<TResult> Items { get; }

    public bool HasPrevious => Index - From > 0;

    public bool HasNext => Index - From + 1 < Pages;
}

/// <summary>
/// Static methods for creating paginated results
/// </summary>
public static class Paginate
{
    /// <summary>
    /// Creates an empty paginated result
    /// </summary>
    /// <typeparam name="T">The type of items</typeparam>
    /// <returns>An empty paginated result</returns>
    public static IPaginate<T> Empty<T>()
    {
        return new Paginate<T>();
    }

    /// <summary>
    /// Creates a paginated result by converting from another paginated result
    /// </summary>
    /// <typeparam name="TResult">The result type</typeparam>
    /// <typeparam name="TSource">The source type</typeparam>
    /// <param name="source">The source paginated result</param>
    /// <param name="converter">The conversion function</param>
    /// <returns>A converted paginated result</returns>
    public static IPaginate<TResult> From<TResult, TSource>(IPaginate<TSource> source,
                                                            Func<IEnumerable<TSource>, IEnumerable<TResult>> converter)
    {
        return new Paginate<TSource, TResult>(source, converter);
    }
}