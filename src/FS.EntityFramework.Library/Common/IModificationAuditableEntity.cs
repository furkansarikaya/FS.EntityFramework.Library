namespace FS.EntityFramework.Library.Common;

/// <summary>
/// Interface for entities that have modification audit properties
/// Includes UpdatedAt and UpdatedBy properties
/// </summary>
public interface IModificationAuditableEntity
{
    /// <summary>
    /// Gets or sets the date and time when the entity was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who last updated the entity
    /// </summary>
    public string? UpdatedBy { get; set; }
}