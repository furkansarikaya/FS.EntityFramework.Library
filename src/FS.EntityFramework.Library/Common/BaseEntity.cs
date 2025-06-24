namespace FS.EntityFramework.Library.Common;

/// <summary>
/// Base entity class that provides the primary key property
/// </summary>
/// <typeparam name="TKey">The type of the entity's primary key</typeparam>
public abstract class BaseEntity<TKey> : IEntity<TKey> where TKey : IEquatable<TKey>
{
    /// <summary>
    /// Gets or sets the primary key of the entity
    /// </summary>
    public TKey Id { get; set; } = default!;
}
