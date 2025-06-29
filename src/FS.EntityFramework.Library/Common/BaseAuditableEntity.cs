namespace FS.EntityFramework.Library.Common;

/// <summary>
/// Base entity class that provides audit properties for tracking entity lifecycle.
/// Includes CreatedAt, CreatedBy, UpdatedAt, UpdatedBy properties.
/// </summary>
/// <typeparam name="TKey">The type of the entity's primary key</typeparam>
public abstract class BaseAuditableEntity<TKey> : BaseEntity<TKey>, ICreationAuditableEntity, IModificationAuditableEntity
    where TKey : IEquatable<TKey>
{
    /// <summary>
    /// Gets or sets the date and time when the entity was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who created the entity
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the entity was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who last updated the entity
    /// </summary>
    public string? UpdatedBy { get; set; }
}