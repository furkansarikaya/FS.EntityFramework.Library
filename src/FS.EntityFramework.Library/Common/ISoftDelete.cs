namespace FS.EntityFramework.Library.Common;

/// <summary>
/// Interface for entities that support soft delete
/// Includes IsDeleted, DeletedAt, and DeletedBy properties
/// </summary>
public interface ISoftDelete
{
    /// <summary>
    /// Gets or sets a value indicating whether the entity is soft deleted
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the entity was soft deleted
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who soft deleted the entity
    /// </summary>
    public string? DeletedBy { get; set; }
}