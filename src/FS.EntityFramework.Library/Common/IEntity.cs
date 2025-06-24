namespace FS.EntityFramework.Library.Common;

/// <summary>
/// Represents an entity with a primary key
/// </summary>
/// <typeparam name="TKey">The type of the entity's primary key</typeparam>
public interface IEntity<TKey> where TKey : IEquatable<TKey>
{
    /// <summary>
    /// Gets or sets the primary key of the entity
    /// </summary>
    TKey Id { get; set; }
}