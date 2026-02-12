namespace FS.EntityFramework.Library.Models;

/// <summary>
/// Defines a reusable, composable filter scope that encapsulates a set of filter criteria.
/// Implement this interface to create predefined filter combinations that can be applied
/// to <see cref="FilterBuilder{T}"/> instances via <c>ApplyScope</c>.
/// <para>
/// Filter scopes promote DRY (Don't Repeat Yourself) by extracting commonly used
/// filter combinations into named, testable, reusable units.
/// </para>
/// <example>
/// <code>
/// public class ActiveProductScope : IFilterScope&lt;Product&gt;
/// {
///     public void Apply(FilterBuilder&lt;Product&gt; builder)
///     {
///         builder
///             .WhereEquals(p => p.IsActive, true)
///             .WhereIsNull(p => p.DeletedAt);
///     }
/// }
///
/// // Usage:
/// var filter = FilterBuilder&lt;Product&gt;.Create()
///     .ApplyScope(new ActiveProductScope())
///     .WhereGreaterThan(p => p.Price, 100m)
///     .Build();
/// </code>
/// </example>
/// </summary>
/// <typeparam name="T">The entity type that this scope applies to.</typeparam>
public interface IFilterScope<T>
{
    /// <summary>
    /// Applies the predefined filter criteria to the given builder.
    /// </summary>
    /// <param name="builder">The filter builder to apply criteria to.</param>
    void Apply(FilterBuilder<T> builder);
}
